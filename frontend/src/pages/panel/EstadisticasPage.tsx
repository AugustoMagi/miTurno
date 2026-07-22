import { useEffect, useState } from 'react'
import { obtenerEstadisticasOcupacion } from '../../api/estadisticas'
import { extractError } from '../../api/client'
import { EstadoReserva } from '../../types/negocio'
import type { EstadisticasOcupacion } from '../../types/estadisticas'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'
import { FieldError } from '../../components/FieldError'
import { validarRangoFechas } from '../../utils/validation'

const ESTADO_LABEL: Record<EstadoReserva, string> = {
  [EstadoReserva.Pendiente]: 'Pendientes',
  [EstadoReserva.Confirmada]: 'Confirmadas',
  [EstadoReserva.Cancelada]: 'Canceladas',
  [EstadoReserva.Completada]: 'Completadas',
}

export function EstadisticasPage() {
  const [desde, setDesde] = useState('')
  const [hasta, setHasta] = useState('')
  const [estadisticas, setEstadisticas] = useState<EstadisticasOcupacion | null>(null)
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)

  function cargar() {
    setCargando(true)
    setError(null)
    obtenerEstadisticasOcupacion(desde || undefined, hasta || undefined)
      .then(setEstadisticas)
      .catch((err) => setError(extractError(err)))
      .finally(() => setCargando(false))
  }

  useEffect(cargar, []) // eslint-disable-line react-hooks/exhaustive-deps

  const errorRango = validarRangoFechas(desde, hasta)

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold text-slate-900">Estadísticas de ocupación</h1>

      <Card>
        <form
          className="flex flex-wrap items-end gap-3"
          onSubmit={(event) => {
            event.preventDefault()
            if (errorRango) return
            cargar()
          }}
        >
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
            Desde
            <input
              type="date"
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
              value={desde}
              onChange={(event) => setDesde(event.target.value)}
            />
          </label>
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
            Hasta
            <input
              type="date"
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
              value={hasta}
              onChange={(event) => setHasta(event.target.value)}
            />
          </label>
          <button
            type="submit"
            disabled={!!errorRango}
            className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Filtrar
          </button>
        </form>
        {errorRango && <FieldError message={errorRango} />}
      </Card>

      {error && <ErrorBanner message={error} />}

      {cargando || !estadisticas ? (
        <Spinner />
      ) : (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Card>
              <p className="text-sm text-slate-500">Ingresos totales</p>
              <p className="mt-1 text-2xl font-semibold text-emerald-700">
                ${estadisticas.ingresosTotales.toLocaleString('es-AR')}
              </p>
            </Card>
            <Card>
              <p className="text-sm text-slate-500">Total de reservas</p>
              <p className="mt-1 text-2xl font-semibold text-slate-900">{estadisticas.totalReservas}</p>
            </Card>
          </div>

          <Card>
            <h2 className="font-semibold text-slate-900">Reservas por estado</h2>
            <div className="mt-3 flex flex-wrap gap-4 text-sm">
              {estadisticas.reservasPorEstado.map((item) => (
                <div key={item.estado}>
                  <p className="text-slate-500">{ESTADO_LABEL[item.estado]}</p>
                  <p className="font-semibold text-slate-900">{item.cantidad}</p>
                </div>
              ))}
            </div>
          </Card>

          <Card>
            <h2 className="font-semibold text-slate-900">Ocupación por recurso</h2>
            <div className="mt-3 flex flex-col gap-3">
              {estadisticas.ocupacionPorRecurso.length === 0 ? (
                <p className="text-sm text-slate-500">Sin datos todavía.</p>
              ) : (
                estadisticas.ocupacionPorRecurso.map((recurso) => (
                  <div key={recurso.recursoId} className="rounded-lg border border-slate-200 px-3 py-2 text-sm">
                    <div className="flex items-center justify-between">
                      <span className="font-medium text-slate-900">{recurso.recursoNombre}</span>
                      <span className="text-slate-500">{recurso.totalReservas} reserva(s)</span>
                    </div>
                    <div className="mt-1 flex flex-wrap gap-3 text-slate-500">
                      {recurso.reservasPorEstado.map((item) => (
                        <span key={item.estado}>
                          {ESTADO_LABEL[item.estado]}: {item.cantidad}
                        </span>
                      ))}
                    </div>
                  </div>
                ))
              )}
            </div>
          </Card>
        </>
      )}
    </div>
  )
}
