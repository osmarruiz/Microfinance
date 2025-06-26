CREATE OR REPLACE FUNCTION update_loan_status_when_all_installments_paid()
    RETURNS TRIGGER AS $$
DECLARE
v_loan_id integer;
    v_total_installments integer;
    v_paid_installments integer;
BEGIN
    -- Si el estado de la cuota no cambió a "Pagada", no hacer nada
    IF NEW.installment_status <> 'Pagada' OR (OLD.installment_status = 'Pagada' AND NEW.installment_status = 'Pagada') THEN
        RETURN NEW;
END IF;

    -- Obtener el ID del préstamo relacionado
    v_loan_id := NEW.loan_id;

    -- Contar el total de cuotas del préstamo (no eliminadas)
SELECT COUNT(*) INTO v_total_installments
FROM business.installments
WHERE loan_id = v_loan_id AND is_deleted = false;

-- Contar las cuotas pagadas del préstamo (no eliminadas)
SELECT COUNT(*) INTO v_paid_installments
FROM business.installments
WHERE loan_id = v_loan_id
  AND installment_status = 'Pagada'
  AND is_deleted = false;

-- Si todas las cuotas están pagadas, actualizar el estado del préstamo
IF v_paid_installments = v_total_installments THEN
UPDATE business.loans
SET loan_status = 'Cancelado'
WHERE loan_id = v_loan_id;
END IF;

RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger para actualizar el estado del préstamo cuando una cuota se marca como pagada
CREATE TRIGGER trg_update_loan_status
    AFTER UPDATE ON business.installments
    FOR EACH ROW
    WHEN (NEW.installment_status = 'Pagada' AND OLD.installment_status <> 'Pagada')
    EXECUTE FUNCTION update_loan_status_when_all_installments_paid();