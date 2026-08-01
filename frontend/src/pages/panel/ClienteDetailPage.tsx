import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { obtenerHistorialCliente } from '../../api/clientes'
import { extractError } from '../../api/client'
import { EstadoReserva } from '../../types/negocio'
import type { HistorialCliente } from '../../types/cliente'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'
import { ArrowLeftIcon } from '../../components/icons'

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

export function ClienteDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [historial, setHistorial] = useState<HistorialCliente | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!id) return
    obtenerHistorialCliente(id)
      .then(setHistorial)
      .catch((err) => setError(extractError(err)))
  }, [id])

  if (error) return <ErrorBanner message={error} />
  if (!historial) return <Spinner />

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link
          to="/panel/clientes"
          className="inline-flex items-center gap-1 text-sm font-medium text-link-600 hover:text-link-700"
        >
          <ArrowLeftIcon className="h-4 w-4" />
          Clientes
        </Link>
        <h1 className="mt-2 text-xl font-semibold text-slate-900">{historial.nombre}</h1>
        <p className="text-sm text-slate-500">
          {historial.email}
          {historial.telefono && ` · ${historial.telefono}`}
        </p>
      </div>

      <div className="flex flex-col gap-3">
        {historial.reservas.length === 0 ? (
          <p className="text-slate-500">Sin reservas todavía.</p>
        ) : (
          historial.reservas
            .slice()
            .sort((a, b) => (a.fecha + a.horaInicio > b.fecha + b.horaInicio ? -1 : 1))
            .map((reserva) => (
              <Card key={reserva.id} hover className="flex items-center justify-between">
                <div>
                  <p className="font-medium text-slate-900">{reserva.recursoNombre}</p>
                  <p className="text-sm text-slate-500">
                    {reserva.fecha} · {formatHora(reserva.horaInicio)} - {formatHora(reserva.horaFin)}
                  </p>
                </div>
                <div className="text-right">
                  <p className="font-semibold text-accent-600">
                    ${reserva.precioTotal.toLocaleString('es-AR')}
                  </p>
                  <span className={`mt-1 inline-block rounded-full px-2 py-0.5 text-xs font-medium ${ESTADO_CLASSES[reserva.estado]}`}>
                    {ESTADO_LABEL[reserva.estado]}
                  </span>
                </div>
              </Card>
            ))
        )}
      </div>
    </div>
  )
}
