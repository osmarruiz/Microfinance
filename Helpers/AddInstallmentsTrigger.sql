CREATE OR REPLACE FUNCTION generate_installments()
    RETURNS TRIGGER AS $$
DECLARE
    installment_count INTEGER;
    installment_num INTEGER := 1;
    due_date TIMESTAMP WITH TIME ZONE;
    nicaragua_date DATE;
    final_due_date TIMESTAMP WITH TIME ZONE;
    monthly_interest NUMERIC(10,2);
    principal_portion NUMERIC(10,2);
    interest_portion NUMERIC(10,2);
    total_interest NUMERIC(10,2);
    remaining_principal NUMERIC(10,2) := NEW.principal_amount;
    remaining_interest NUMERIC(10,2);
    days_interval INTEGER;
    is_weekend BOOLEAN;
    base_principal INTEGER;
    principal_remainder NUMERIC(10,2);
    base_interest INTEGER;
    interest_remainder NUMERIC(10,2);
BEGIN
    -- Validar parámetros del préstamo
    IF NEW.principal_amount <= 0 THEN
        RAISE EXCEPTION 'El monto principal debe ser positivo';
    END IF;

    IF NEW.term_months <= 0 THEN
        RAISE EXCEPTION 'El plazo debe ser positivo';
    END IF;

    -- Usar la tasa de interés mensual directamente de la tabla
    monthly_interest := NEW.monthly_interest_rate / 100;

    -- Calcular interés total para el préstamo (interés simple)
    total_interest := ROUND(NEW.principal_amount * monthly_interest * NEW.term_months, 2);
    remaining_interest := total_interest;

    -- Determinar número de cuotas e intervalo basado en la frecuencia
    CASE NEW.payment_frequency
        WHEN 'Diario' THEN
            installment_count := NEW.term_months * 30; -- Aproximado
            days_interval := 1;
        WHEN 'Semanal' THEN
            installment_count := NEW.term_months * 4;
            days_interval := 7;
        WHEN 'Quincenal' THEN
            installment_count := NEW.term_months * 2;
            days_interval := 15;
        WHEN 'Mensual' THEN
            installment_count := NEW.term_months;
            days_interval := 30;
        END CASE;

    -- Calcular partes enteras del principal
    base_principal := FLOOR(NEW.principal_amount / installment_count);
    principal_remainder := NEW.principal_amount - (base_principal * installment_count);

    -- Calcular partes enteras del interés
    base_interest := FLOOR(total_interest / installment_count);
    interest_remainder := total_interest - (base_interest * installment_count);

    -- Comenzar con la fecha de inicio en hora de Nicaragua
    due_date := NEW.start_date;
    nicaragua_date := (due_date AT TIME ZONE 'UTC' AT TIME ZONE 'America/Managua')::DATE;

    -- Para pagos diarios, encontrar el próximo día laboral si es domingo
    IF NEW.payment_frequency = 'Diario' THEN
        WHILE EXTRACT(DOW FROM nicaragua_date) = 0 LOOP -- Domingo
        nicaragua_date := nicaragua_date + INTERVAL '1 day';
            END LOOP;
        due_date := (nicaragua_date::TEXT || ' 00:00:00 America/Managua')::TIMESTAMP WITH TIME ZONE;
    END IF;

    FOR i IN 1..installment_count LOOP
            -- Para frecuencias no diarias, calcular la próxima fecha de pago
            IF NEW.payment_frequency != 'Diario' THEN
                due_date := due_date + (days_interval || ' days')::INTERVAL;
            END IF;

            -- Convertir a fecha de Nicaragua para verificar día laboral
            nicaragua_date := (due_date AT TIME ZONE 'UTC' AT TIME ZONE 'America/Managua')::DATE;

            -- Verificar si la fecha cae en domingo (0) en Nicaragua
            is_weekend := EXTRACT(DOW FROM nicaragua_date) = 0;

            -- Ajustar fecha si cae en domingo (mover al próximo lunes)
            WHILE is_weekend LOOP
                    nicaragua_date := nicaragua_date + INTERVAL '1 day';
                    is_weekend := EXTRACT(DOW FROM nicaragua_date) = 0;
                END LOOP;

            -- Para pagos diarios, avanzar al siguiente día (ya se saltaron domingos)
            IF NEW.payment_frequency = 'Diario' AND i > 1 THEN
                nicaragua_date := nicaragua_date + INTERVAL '1 day';
                -- Saltar domingo si caemos en él
                IF EXTRACT(DOW FROM nicaragua_date) = 0 THEN
                    nicaragua_date := nicaragua_date + INTERVAL '1 day';
                END IF;
            END IF;

            -- Convertir de vuelta a timestamp con zona horaria
            due_date := (nicaragua_date::TEXT || ' 00:00:00 America/Managua')::TIMESTAMP WITH TIME ZONE;

            -- Guardar la última fecha de vencimiento para actualizar el préstamo
            IF i = installment_count THEN
                final_due_date := due_date;
            END IF;

            -- Calcular porciones (todas enteras excepto posiblemente la última)
            IF i < installment_count THEN
                principal_portion := base_principal;
                interest_portion := base_interest;
            ELSE
                -- Última cuota lleva todo el remanente
                principal_portion := remaining_principal;
                interest_portion := remaining_interest;
            END IF;

            -- Insertar cuota con fechas corregidas para Nicaragua
            INSERT INTO business.installments (
                loan_id,
                installment_number,
                principal_amount,
                normal_interest_amount,
                late_interest_amount,
                paid_amount,
                due_date,
                payment_date,
                installment_status,
                is_deleted
            ) VALUES (
                         NEW.loan_id,
                         installment_num,
                         ROUND(principal_portion, 2),
                         ROUND(interest_portion, 2),
                         0,
                         0,
                         due_date AT TIME ZONE 'UTC',  -- Almacenado en UTC
                         NULL,                         -- Fecha de pago inicialmente nula
                         'Pendiente',
                         false
                     );

            -- Actualizar variables de seguimiento
            remaining_principal := remaining_principal - principal_portion;
            remaining_interest := remaining_interest - interest_portion;
            installment_num := installment_num + 1;

            -- Para pagos diarios, actualizar due_date para la siguiente iteración
            IF NEW.payment_frequency = 'Diario' THEN
                due_date := (nicaragua_date::TEXT || ' 00:00:00 America/Managua')::TIMESTAMP WITH TIME ZONE;
            END IF;
        END LOOP;

    -- Actualizar la fecha de vencimiento final en la tabla de préstamos
    UPDATE business.loans
    SET due_date = final_due_date AT TIME ZONE 'UTC'
    WHERE loan_id = NEW.loan_id;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger
CREATE TRIGGER trg_generate_installments
    AFTER INSERT ON business.loans
    FOR EACH ROW
EXECUTE FUNCTION generate_installments();