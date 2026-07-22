import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { activarNegocio, desactivarNegocio, obtenerNegocioDetalle } from '../../api/negociosAdmin'
import { extractError } from '../../api/client'
import { EstadoReserva } from '../../types/negocio'
import { DIAS_SEMANA } from '../../types/horario'
import type { NegocioDetalleAdmin } from '../../types/negocioAdmin'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'

function formatHora(horaHms: string): string {
  return horaHms.slice(0, 5)
}

function nombreDia(diaSemana: number): string {
  return DIAS_SEMANA.find((d) => d.valor === diaSemana)?.nombre ?? String(diaSemana)
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

export function NegocioDetailPage() {
  const { id } = useParams<{ id: string }>()

  const [negocio, setNegocio] = useState<NegocioDetalleAdmin | null>(null)
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [procesando, setProcesando] = useState(false)

  function cargar() {
    if (!id) return
    setCargando(true)
    setError(null)
    obtenerNegocioDetalle(id)
      .then(setNegocio)
      .catch((err) => setError(extractError(err)))
      .finally(() => setCargando(false))
  }

  useEffect(cargar, [id]) // eslint-disable-line react-hooks/exhaustive-deps

  async function handleCambiarEstado() {
    if (!negocio) return
    setProcesando(true)
    setError(null)
    try {
      if (negocio.activo) await desactivarNegocio(negocio.id)
      else await activarNegocio(negocio.id)
      cargar()
    } catch (err) {
      setError(extractError(err))
    } finally {
      setProcesando(false)
    }
  }

  if (cargando) return <Spinner />
  if (error || !negocio) return <ErrorBanner message={error ?? 'Negocio no encontrado.'} />

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link to="/admin/negocios" className="text-sm text-emerald-700 hover:underline">
          ← Negocios
        </Link>
        <div className="mt-2 flex flex-wrap items-center gap-2">
          <h1 className="text-xl font-semibold text-slate-900">{negocio.nombre}</h1>
          <span
            className={`rounded-full px-2 py-0.5 text-xs font-medium ${
              negocio.activo ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'
            }`}
          >
            {negocio.activo ? 'Activo' : 'Inactivo'}
          </span>
        </div>
        <p className="text-sm text-slate-500">
          {negocio.email} · miturno.app/{negocio.slug}
        </p>
      </div>

      <div>
        <button
          type="button"
          disabled={procesando}
          onClick={handleCambiarEstado}
          className="rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {negocio.activo ? 'Desactivar negocio' : 'Activar negocio'}
        </button>
      </div>

      {negocio.recursos.length === 0 ? (
        <p className="text-slate-500">Este negocio todavía no cargó recursos.</p>
      ) : (
        negocio.recursos.map((recurso) => (
          <Card key={recurso.id} className="flex flex-col gap-4">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="font-semibold text-slate-900">{recurso.nombre}</h2>
              <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">
                {recurso.tipo}
              </span>
              <span
                className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                  recurso.activo ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'
                }`}
              >
                {recurso.activo ? 'Activo' : 'Inactivo'}
              </span>
              <span className="text-sm text-slate-500">
                {recurso.duracionTurnoMinutos} min · ${recurso.precio.toLocaleString('es-AR')}
              </span>
            </div>

            <div>
              <h3 className="text-sm font-semibold text-slate-700">Horarios disponibles</h3>
              {recurso.horarios.length === 0 ? (
                <p className="mt-1 text-sm text-slate-500">Sin horarios cargados.</p>
              ) : (
                <div className="mt-2 flex flex-wrap gap-2">
                  {recurso.horarios.map((horario) => (
                    <span
                      key={horario.id}
                      className="rounded-lg border border-slate-200 px-3 py-1.5 text-sm text-slate-600"
                    >
                      {nombreDia(horario.diaSemana)} {formatHora(horario.horaInicio)}-{formatHora(horario.horaFin)}
                    </span>
                  ))}
                </div>
              )}
            </div>

            <div>
              <h3 className="text-sm font-semibold text-slate-700">Reservas</h3>
              {recurso.reservas.length === 0 ? (
                <p className="mt-1 text-sm text-slate-500">Sin reservas todavía.</p>
              ) : (
                <div className="mt-2 flex flex-col gap-2">
                  {recurso.reservas.map((reserva) => (
                    <div
                      key={reserva.id}
                      className="flex flex-col gap-1 rounded-lg border border-slate-200 px-3 py-2 text-sm sm:flex-row sm:items-center sm:justify-between"
                    >
                      <div className="flex items-center gap-2">
                        <span
                          className={`rounded-full px-2 py-0.5 text-xs font-medium ${ESTADO_CLASSES[reserva.estado]}`}
                        >
                          {ESTADO_LABEL[reserva.estado]}
                        </span>
                        <span className="text-slate-700">
                          {reserva.fecha} · {formatHora(reserva.horaInicio)}-{formatHora(reserva.horaFin)}
                        </span>
                      </div>
                      <div className="flex items-center gap-2 text-slate-500">
                        <span>
                          {reserva.clienteNombre} ({reserva.clienteEmail})
                        </span>
                        <span className="font-medium text-emerald-700">
                          ${reserva.precioTotal.toLocaleString('es-AR')}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </Card>
        ))
      )}
    </div>
  )
}
