import { useEffect, useState } from 'react'
import {
  cancelarReserva,
  confirmarPagoReserva,
  listarReservas,
  rechazarPagoReserva,
} from '../../api/reservasOwner'
import { extractError } from '../../api/client'
import { EstadoReserva } from '../../types/negocio'
import type { ReservaOwner } from '../../types/reservaOwner'
import { Card } from '../../components/Card'
import { Button } from '../../components/Button'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'
import { Input } from '../../components/Input'

function formatHora(horaHms: string): string {
  return horaHms.slice(0, 5)
}

const ESTADO_LABEL: Record<EstadoReserva, string> = {
  [EstadoReserva.Pendiente]: 'Pendiente',
  [EstadoReserva.Confirmada]: 'Confirmada',
  [EstadoReserva.Cancelada]: 'Cancelada',
  [EstadoReserva.Completada]: 'Completada',
}

const ESTADO_CLASSES: Record<EstadoReserva, string> = {
  [EstadoReserva.Pendiente]: 'bg-amber-50 text-amber-700',
  [EstadoReserva.Confirmada]: 'bg-emerald-50 text-emerald-700',
  [EstadoReserva.Cancelada]: 'bg-red-50 text-red-700',
  [EstadoReserva.Completada]: 'bg-slate-100 text-slate-600',
}

export function ReservasListPage() {
  const [reservas, setReservas] = useState<ReservaOwner[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [procesando, setProcesando] = useState<string | null>(null)
  const [filtroFecha, setFiltroFecha] = useState('')

  function cargar() {
    setError(null)
    listarReservas()
      .then(setReservas)
      .catch((err) => setError(extractError(err)))
  }

  useEffect(cargar, [])

  async function handleAccion(id: string, accion: () => Promise<void>) {
    setProcesando(id)
    setError(null)
    try {
      await accion()
      cargar()
    } catch (err) {
      setError(extractError(err))
    } finally {
      setProcesando(null)
    }
  }

  if (!reservas) return <Spinner />

  const reservasFiltradas = reservas
    .filter((reserva) => !filtroFecha || reserva.fecha === filtroFecha)
    .sort((a, b) => `${b.fecha}${b.horaInicio}`.localeCompare(`${a.fecha}${a.horaInicio}`))

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-xl font-semibold text-slate-900">Reservas</h1>
        <div className="flex items-center gap-2">
          <Input
            type="date"
            aria-label="Filtrar por día"
            className="w-auto"
            value={filtroFecha}
            onChange={(event) => setFiltroFecha(event.target.value)}
          />
          {filtroFecha && (
            <button
              type="button"
              onClick={() => setFiltroFecha('')}
              className="text-sm font-medium text-link-600 hover:text-link-700 hover:underline"
            >
              Ver todas
            </button>
          )}
        </div>
      </div>

      {error && <ErrorBanner message={error} />}

      {reservas.length === 0 ? (
        <p className="text-slate-500">Todavía no hay reservas.</p>
      ) : reservasFiltradas.length === 0 ? (
        <p className="text-slate-500">No hay reservas ese día.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {reservasFiltradas.map((reserva) => (
            <Card
              key={reserva.id}
              hover
              className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"
            >
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-slate-900">{reserva.recursoNombre}</span>
                  <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${ESTADO_CLASSES[reserva.estado]}`}>
                    {ESTADO_LABEL[reserva.estado]}
                  </span>
                </div>
                <p className="text-sm text-slate-500">
                  {reserva.fecha} · {formatHora(reserva.horaInicio)} - {formatHora(reserva.horaFin)}
                </p>
                <p className="text-sm text-slate-500">
                  {reserva.clienteNombre} ({reserva.clienteEmail})
                </p>
                <p className="text-sm font-semibold text-accent-600">
                  ${reserva.precioTotal.toLocaleString('es-AR')}
                </p>
              </div>
              <div className="flex flex-wrap gap-2">
                {reserva.estado === EstadoReserva.Pendiente && (
                  <>
                    <Button
                      variant="secondary"
                      size="sm"
                      loading={procesando === reserva.id}
                      onClick={() => handleAccion(reserva.id, () => confirmarPagoReserva(reserva.id))}
                      className="border-emerald-300 text-emerald-700 hover:bg-emerald-50"
                    >
                      Confirmar pago
                    </Button>
                    <Button
                      variant="secondary"
                      size="sm"
                      loading={procesando === reserva.id}
                      onClick={() => handleAccion(reserva.id, () => rechazarPagoReserva(reserva.id))}
                      className="border-red-300 text-red-600 hover:bg-red-50"
                    >
                      Rechazar pago
                    </Button>
                  </>
                )}
                {reserva.estado !== EstadoReserva.Cancelada && reserva.estado !== EstadoReserva.Completada && (
                  <Button
                    variant="secondary"
                    size="sm"
                    loading={procesando === reserva.id}
                    onClick={() => handleAccion(reserva.id, () => cancelarReserva(reserva.id))}
                  >
                    Cancelar
                  </Button>
                )}
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
