CREATE OR REPLACE FUNCTION calculate_late_interest()
    RETURNS TRIGGER AS $$
DECLARE
    v_monthly_interest_rate     NUMERIC(10,4);
    v_effective_annual_rate     NUMERIC(10,6);
    v_late_annual_rate          NUMERIC(10,6);
    v_daily_late_rate           NUMERIC(10,8);
    v_days_late                 INTEGER;
    v_principal_due             NUMERIC(10,2);
BEGIN
    -- Evitar cálculos si ya está pagada
    IF NEW.payment_date IS NOT NULL THEN
        NEW.installment_status := 'Pagada';
        RETURN NEW;
    END IF;

    -- Validar si está vencida o en mora
    IF (NEW.installment_status = 'Vencida' AND NEW.due_date < CURRENT_DATE) THEN

        -- Obtener la tasa de interés mensual del préstamo
        BEGIN
            SELECT monthly_interest_rate INTO STRICT v_monthly_interest_rate
            FROM business.loans
            WHERE loan_id = NEW.loan_id;
        EXCEPTION WHEN NO_DATA_FOUND THEN
            RAISE EXCEPTION 'No se encontró el préstamo con ID: %', NEW.loan_id;
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error al obtener tasa de interés para préstamo %: %',
                NEW.loan_id, SQLERRM;
        END;

        -- 1. Calcular tasa efectiva anual
        v_effective_annual_rate := POWER(1 + (v_monthly_interest_rate / 100), 12) - 1;

        -- 2. Calcular tasa moratoria anual (Art. 73)
        v_late_annual_rate := v_effective_annual_rate * 1.25;

        -- 3. Convertir a tasa diaria efectiva
        v_daily_late_rate := POWER(1 + v_late_annual_rate, 1.0 / 365) - 1;

        -- 4. Calcular días de atraso (mínimo 1 día)
        v_days_late := GREATEST((CURRENT_DATE - NEW.due_date::date), 1);

        -- 5. Calcular capital pendiente (cuota sin pagar)
        v_principal_due := GREATEST(
                COALESCE(NEW.principal_amount, 0) +
                COALESCE(NEW.normal_interest_amount, 0) 
        
                           );

        -- 6. Calcular interés moratorio
        NEW.late_interest_amount := ROUND(v_principal_due * v_daily_late_rate * v_days_late, 2);

        -- Mensaje de depuración
        RAISE NOTICE 'Cuota % en mora: C$%, % días vencida, tasa diaria efectiva: %',
            NEW.installment_id, NEW.late_interest_amount, v_days_late, v_daily_late_rate;

    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;


-- Trigger para operaciones manuales
CREATE OR REPLACE TRIGGER trg_calculate_late_interest
    BEFORE INSERT OR UPDATE ON business.installments
    FOR EACH ROW
EXECUTE FUNCTION calculate_late_interest();

