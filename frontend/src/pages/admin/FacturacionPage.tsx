import { useEffect, useState } from 'react'
import { obtenerFacturacion } from '../../api/facturacionAdmin'
import { extractError } from '../../api/client'
import type { FacturacionPlataforma } from '../../types/facturacionAdmin'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'
import { FieldError } from '../../components/FieldError'
import { validarRangoFechas } from '../../utils/validation'

export function FacturacionPage() {
  const [desde, setDesde] = useState('')
  const [hasta, setHasta] = useState('')
  const [facturacion, setFacturacion] = useState<FacturacionPlataforma | null>(null)
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)

  function cargar() {
    setCargando(true)
    setError(null)
    obtenerFacturacion(desde || undefined, hasta || undefined)
      .then(setFacturacion)
      .catch((err) => setError(extractError(err)))
      .finally(() => setCargando(false))
  }

  useEffect(cargar, []) // eslint-disable-line react-hooks/exhaustive-deps

  const errorRango = validarRangoFechas(desde, hasta)

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold text-slate-900">Facturación</h1>
      <p className="-mt-4 text-sm text-slate-500">
        Ingresos que MiTurno cobró a los negocios por sus suscripciones (pagos aprobados). No
        incluye lo que cada negocio le cobra a sus propios clientes por reservas.
      </p>

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

      {cargando || !facturacion ? (
        <Spinner />
      ) : (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Card>
              <p className="text-sm text-slate-500">Total facturado</p>
              <p className="mt-1 text-2xl font-semibold text-emerald-700">
                ${facturacion.totalFacturado.toLocaleString('es-AR')}
              </p>
            </Card>
            <Card>
              <p className="text-sm text-slate-500">Cantidad de pagos</p>
              <p className="mt-1 text-2xl font-semibold text-slate-900">{facturacion.cantidadPagos}</p>
            </Card>
          </div>

          <Card>
            <h2 className="font-semibold text-slate-900">Facturación por plan</h2>
            {facturacion.porPlan.length === 0 ? (
              <p className="mt-3 text-sm text-slate-500">Sin pagos en el período elegido.</p>
            ) : (
              <div className="mt-3 flex flex-col gap-2">
                {facturacion.porPlan.map((item) => (
                  <div
                    key={item.planId}
                    className="flex items-center justify-between rounded-lg border border-slate-200 px-3 py-2 text-sm"
                  >
                    <span className="font-medium text-slate-900">{item.planNombre}</span>
                    <span className="text-slate-500">{item.cantidadPagos} pago(s)</span>
                    <span className="font-semibold text-emerald-700">
                      ${item.total.toLocaleString('es-AR')}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </Card>

          <Card>
            <h2 className="font-semibold text-slate-900">Facturación por negocio</h2>
            {facturacion.porNegocio.length === 0 ? (
              <p className="mt-3 text-sm text-slate-500">Sin pagos en el período elegido.</p>
            ) : (
              <div className="mt-3 flex flex-col gap-2">
                {facturacion.porNegocio.map((item) => (
                  <div
                    key={item.negocioId}
                    className="flex items-center justify-between rounded-lg border border-slate-200 px-3 py-2 text-sm"
                  >
                    <span className="font-medium text-slate-900">{item.negocioNombre}</span>
                    <span className="text-slate-500">{item.cantidadPagos} pago(s)</span>
                    <span className="font-semibold text-emerald-700">
                      ${item.total.toLocaleString('es-AR')}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </Card>
        </>
      )}
    </div>
  )
}
