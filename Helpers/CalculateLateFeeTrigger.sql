CREATE OR REPLACE FUNCTION calculate_late_interest()
    RETURNS TRIGGER AS $$
DECLARE
    v_monthly_interest_rate     NUMERIC(10,4);
    v_effective_annual_rate     NUMERIC(10,6);
    v_late_annual_rate          NUMERIC(10,6);
    v_daily_late_rate           NUMERIC(10,8);
    v_days_late                 INTEGER;
    v_principal_due             NUMERIC(10,2);
    integer_part                NUMERIC;
    decimal_part                NUMERIC;
BEGIN
    -- Inicializar interés moratorio a 0 por defecto
    NEW.late_interest_amount := 0.00;

    -- Solo calcular si está vencida y no pagada
    IF NEW.installment_status = 'Vencida' AND NEW.due_date < CURRENT_DATE AND NEW.payment_date IS NULL THEN
        -- Obtener la tasa de interés mensual del préstamo
        BEGIN
            SELECT monthly_interest_rate INTO STRICT v_monthly_interest_rate
            FROM business.loans
            WHERE loan_id = NEW.loan_id;

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
                    COALESCE(NEW.normal_interest_amount, 0),
                    0
                               );

            -- 6. Calcular interés moratorio
            NEW.late_interest_amount := v_principal_due * v_daily_late_rate * v_days_late;

            -- Mensaje de depuración
            RAISE NOTICE 'Cuota % en mora: C$%, % días vencida, tasa diaria efectiva: %',
                NEW.installment_id, NEW.late_interest_amount, v_days_late, v_daily_late_rate;

        EXCEPTION WHEN NO_DATA_FOUND THEN
            RAISE NOTICE 'No se encontró el préstamo con ID: %', NEW.loan_id;
        WHEN OTHERS THEN
            RAISE NOTICE 'Error al calcular interés moratorio para préstamo %: %',
                NEW.loan_id, SQLERRM;
        END;
    END IF;

    -- Aplicar redondeo en todos los casos
    integer_part := TRUNC(NEW.late_interest_amount);
    decimal_part := NEW.late_interest_amount - integer_part;

    IF decimal_part <= 0.39 THEN
        NEW.late_interest_amount := integer_part + 0.00;
    ELSIF decimal_part >= 0.60 THEN
        NEW.late_interest_amount := integer_part + 1.00;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger para operaciones manuales
CREATE OR REPLACE TRIGGER trg_calculate_late_interest
    BEFORE INSERT OR UPDATE ON business.installments
    FOR EACH ROW
EXECUTE FUNCTION calculate_late_interest();

