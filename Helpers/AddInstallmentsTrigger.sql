CREATE OR REPLACE FUNCTION generate_installments()
    RETURNS TRIGGER AS $$
DECLARE
    i INTEGER := 0;
    total_installments INTEGER;
    installment_amount NUMERIC(10,2);
    total_payment NUMERIC(10,2);
    due_date TIMESTAMP WITH TIME ZONE;
    payment_date_candidate TIMESTAMP WITH TIME ZONE;
    max_daily_installments INTEGER := 20;
    collector_installment_count INTEGER;
    is_weekend BOOLEAN;
    business_days_needed INTEGER;
    calc_date TIMESTAMP WITH TIME ZONE;
    remaining_amount NUMERIC(10,2);
    first_payment_date TIMESTAMP WITH TIME ZONE;
    monthly_interest_rate NUMERIC(10,2);
BEGIN
    -- Calcular tasa de interés mensual
    monthly_interest_rate := NEW.interest_rate/100;

    -- Calcular el monto total a pagar con interés compuesto mensual
    total_payment := NEW.amount * POWER(1 + monthly_interest_rate, NEW.term_months);

    -- Actualizar el saldo pendiente en el préstamo
    UPDATE business.loans
    SET current_balance = total_payment
    WHERE loan_id = NEW.loan_id;

    -- Determinar fecha de primer pago (mañana si es hoy)
    first_payment_date := NEW.start_date + INTERVAL '1 day';

    -- Determinar el número total de cuotas según la frecuencia
    CASE NEW.payment_frequency
        WHEN 'Diario' THEN
            -- Calcular días laborales necesarios (30 días naturales ≈ 26 días laborales por mes)
            business_days_needed := NEW.term_months * 26;
            total_installments := business_days_needed;

        WHEN 'Semanal' THEN
            total_installments := NEW.term_months * 4; -- 4 semanas por mes
        WHEN 'Quincenal' THEN
            total_installments := NEW.term_months * 2; -- 2 quincenas por mes
        WHEN 'Mensual' THEN
            total_installments := NEW.term_months; -- 1 cuota por mes
        ELSE
            total_installments := NEW.term_months;
        END CASE;

    -- Calcular el monto de cada cuota (redondeado a 2 decimales)
    installment_amount := ROUND(total_payment / total_installments, 2);

    -- Ajustar la última cuota para compensar redondeos
    remaining_amount := total_payment - (installment_amount * (total_installments - 1));

    -- Generar las cuotas
    calc_date := first_payment_date; -- Comenzar desde mañana
    WHILE i < total_installments LOOP
            -- Para frecuencia diaria, avanzar día por día excluyendo domingos
            IF NEW.payment_frequency = 'Diario' THEN
                -- Saltar domingos
                WHILE EXTRACT(DOW FROM calc_date) = 0 LOOP
                        calc_date := calc_date + INTERVAL '1 day';
                    END LOOP;

                due_date := calc_date;
                calc_date := calc_date + INTERVAL '1 day';
            ELSE
                -- Para otras frecuencias
                i := i + 1;
                CASE NEW.payment_frequency
                    WHEN 'Semanal' THEN
                        due_date := first_payment_date + (i * INTERVAL '1 week');
                    WHEN 'Quincenal' THEN
                        due_date := first_payment_date + (i * INTERVAL '15 days');
                    WHEN 'Mensual' THEN
                        due_date := first_payment_date + (i * INTERVAL '1 month');
                    ELSE
                        due_date := first_payment_date + (i * INTERVAL '1 month');
                    END CASE;
            END IF;

            -- Ajustar fecha si es sábado (para frecuencias no diarias)
            IF NEW.payment_frequency <> 'Diario' THEN
                is_weekend := EXTRACT(DOW FROM due_date) IN (0, 6);
                WHILE is_weekend LOOP
                        due_date := due_date + INTERVAL '1 day';
                        is_weekend := EXTRACT(DOW FROM due_date) IN (0, 6);
                    END LOOP;
            END IF;

            -- Buscar fecha de pago adecuada
            payment_date_candidate := due_date;

            -- Verificar carga del cobrador
            SELECT COUNT(*) INTO collector_installment_count
            FROM business.installments
            WHERE payment_date = payment_date_candidate::date
              AND loan_id IN (
                SELECT loan_id FROM business.loans WHERE seller_id = NEW.seller_id
            );

            -- Ajustar si hay sobrecarga
            WHILE collector_installment_count >= max_daily_installments LOOP
                    payment_date_candidate := payment_date_candidate + INTERVAL '1 day';
                    WHILE EXTRACT(DOW FROM payment_date_candidate) IN (0, 6) LOOP
                            payment_date_candidate := payment_date_candidate + INTERVAL '1 day';
                        END LOOP;
                    SELECT COUNT(*) INTO collector_installment_count
                    FROM business.installments
                    WHERE payment_date = payment_date_candidate::date
                      AND loan_id IN (
                        SELECT loan_id FROM business.loans WHERE seller_id = NEW.seller_id
                    );
                END LOOP;

            -- Insertar cuota
            IF NEW.payment_frequency = 'Diario' OR i > 0 THEN
                INSERT INTO business.installments (
                    loan_id,
                    installment_number,
                    installment_amount,
                    paid_amount,
                    late_fee,
                    due_date,
                    payment_date,
                    installment_status
                ) VALUES (
                             NEW.loan_id,
                             CASE WHEN NEW.payment_frequency = 'Diario' THEN i + 1 ELSE i END,
                             CASE WHEN (NEW.payment_frequency = 'Diario' AND i + 1 = total_installments)
                                 OR i = total_installments
                                      THEN remaining_amount
                                  ELSE installment_amount END,
                             0,
                             0,
                             due_date,
                             payment_date_candidate,
                             'Pendiente'
                         );

                IF NEW.payment_frequency = 'Diario' THEN
                    i := i + 1;
                END IF;
            END IF;
        END LOOP;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;