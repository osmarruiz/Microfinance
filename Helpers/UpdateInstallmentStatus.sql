CREATE OR REPLACE FUNCTION update_installment_status()
    RETURNS TRIGGER AS $$
DECLARE
    v_installment RECORD;
    v_total_paid NUMERIC(10,2);
    v_total_due NUMERIC(10,2);
    v_current_balance NUMERIC(10,2);
BEGIN
    -- Obtener todos los datos de la cuota en una sola consulta
    SELECT * INTO v_installment
    FROM business.installments
    WHERE installment_id = NEW.installment_id
        FOR UPDATE; -- Bloquea el registro para evitar condiciones de carrera

    -- Calcular total pagado (solo pagos no eliminados)
    -- Para INSERT: excluir el nuevo pago (que aún no está en la tabla)
    -- Para UPDATE: excluir el pago actual que se está modificando
    SELECT COALESCE(SUM(paid_amount), 0)
    INTO v_total_paid
    FROM business.payments
    WHERE installment_id = NEW.installment_id
      AND is_deleted = false
      AND payment_id != COALESCE(NEW.payment_id, 0); -- Excluye el pago actual

    -- Calcular total adeudado
    v_total_due := v_installment.principal_amount +
                   v_installment.normal_interest_amount +
                   v_installment.late_interest_amount;

    -- Calcular saldo pendiente antes del nuevo pago
    v_current_balance := v_total_due - v_total_paid;

    -- Validar que el pago no exceda el saldo pendiente
    IF NEW.paid_amount > v_current_balance THEN
        RAISE EXCEPTION 'El pago de % excede el saldo pendiente de %',
            NEW.paid_amount, v_current_balance;
    END IF;

    -- Calcular nuevo total pagado (sumar el nuevo pago)
    v_total_paid := v_total_paid + NEW.paid_amount;

    -- Actualización en una sola operación
    UPDATE business.installments
    SET
        paid_amount = v_total_paid,
        installment_status = CASE
                                 WHEN v_total_paid >= v_total_due THEN 'Pagada'
                                 ELSE v_installment.installment_status END,
        payment_date = CASE
                           WHEN v_total_paid >= v_total_due THEN NEW.payment_date
                           ELSE v_installment.payment_date END
    WHERE installment_id = NEW.installment_id;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger para actualizar el estado de la cuota después de insertar un pago
CREATE TRIGGER trg_update_installment_status
    AFTER INSERT ON business.payments
    FOR EACH ROW
    EXECUTE FUNCTION update_installment_status();