CREATE OR REPLACE FUNCTION update_loan_totals()
    RETURNS TRIGGER AS $$
BEGIN
    -- Actualizar el préstamo cuando cambian las cuotas
UPDATE business.loans l
SET
    principal_amount = subquery.sum_principal,
    normal_interest_amount = subquery.sum_interest,
    late_interest_amount = subquery.sum_late_interest
    FROM (
        SELECT 
            SUM(principal_amount) as sum_principal,
            SUM(normal_interest_amount) as sum_interest,
            SUM(late_interest_amount) as sum_late_interest
        FROM business.installments
        WHERE loan_id = COALESCE(NEW.loan_id, OLD.loan_id)
    ) subquery
WHERE l.loan_id = COALESCE(NEW.loan_id, OLD.loan_id);

RETURN NULL; -- Es un trigger AFTER, no necesitamos devolver la fila
END;
$$ LANGUAGE plpgsql;

-- Trigger para mantener actualizados los totales del préstamo
CREATE TRIGGER trg_update_loan_totals
    AFTER INSERT OR UPDATE OR DELETE ON business.installments
    FOR EACH ROW
    EXECUTE FUNCTION update_loan_totals();