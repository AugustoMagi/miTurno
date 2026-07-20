import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { obtenerHistorialCliente } from '../../api/clientes'
import { extractError } from '../../api/client'
import { EstadoReserva } from '../../types/negocio'
import type { HistorialCliente } from '../../types/cliente'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'

function formatHora(horaHms: string): string {
  return horaHms.slice(0, 5)
}

const ESTADO_LABEL: Record<EstadoReserva, string> = {
  [EstadoReserva.Pendiente]: 'Pendiente',
  [EstadoReserva.Confirmada]: 'Confirmada',
  [EstadoReserva.Cancelada]: 'Cancelada',
  [EstadoReserva.Completada]: 'Completada',
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
        <Link to="/panel/clientes" className="text-sm text-emerald-700 hover:underline">
          ← Clientes
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
              <Card key={reserva.id} className="flex items-center justify-between">
                <div>
                  <p className="font-medium text-slate-900">{reserva.recursoNombre}</p>
                  <p className="text-sm text-slate-500">
                    {reserva.fecha} · {formatHora(reserva.horaInicio)} - {formatHora(reserva.horaFin)}
                  </p>
                </div>
                <div className="text-right text-sm">
                  <p className="font-medium text-emerald-700">
                    ${reserva.precioTotal.toLocaleString('es-AR')}
                  </p>
                  <p className="text-slate-500">{ESTADO_LABEL[reserva.estado]}</p>
                </div>
              </Card>
            ))
        )}
      </div>
    </div>
  )
}
