import { useEffect, useState } from 'react'
import { obtenerFacturacion } from '../../api/facturacionAdmin'
import { extractError } from '../../api/client'
import type { FacturacionPlataforma } from '../../types/facturacionAdmin'
import { Card } from '../../components/Card'
import { Button } from '../../components/Button'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'
import { Field, Input } from '../../components/Input'
import { CreditCardIcon, ReceiptIcon } from '../../components/icons'
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
          <Field label="Desde">
            <Input type="date" value={desde} onChange={(event) => setDesde(event.target.value)} className="w-auto" />
          </Field>
          <Field label="Hasta">
            <Input type="date" value={hasta} onChange={(event) => setHasta(event.target.value)} className="w-auto" />
          </Field>
          <Button type="submit" disabled={!!errorRango}>
            Filtrar
          </Button>
        </form>
        {errorRango && <p className="mt-2 text-xs font-normal text-red-600">{errorRango}</p>}
      </Card>

      {error && <ErrorBanner message={error} />}

      {cargando || !facturacion ? (
        <Spinner />
      ) : (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Card className="flex items-center gap-4">
              <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-accent-50 text-accent-600">
                <CreditCardIcon className="h-5 w-5" />
              </span>
              <div>
                <p className="text-sm text-slate-500">Total facturado</p>
                <p className="mt-0.5 text-2xl font-bold text-slate-900" style={{ fontFamily: 'var(--font-heading)' }}>
                  ${facturacion.totalFacturado.toLocaleString('es-AR')}
                </p>
              </div>
            </Card>
            <Card className="flex items-center gap-4">
              <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-link-50 text-link-600">
                <ReceiptIcon className="h-5 w-5" />
              </span>
              <div>
                <p className="text-sm text-slate-500">Cantidad de pagos</p>
                <p className="mt-0.5 text-2xl font-bold text-slate-900" style={{ fontFamily: 'var(--font-heading)' }}>
                  {facturacion.cantidadPagos}
                </p>
              </div>
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
                    className="flex items-center justify-between rounded-xl border border-slate-200 px-4 py-2.5 text-sm transition-colors duration-200 hover:bg-slate-50"
                  >
                    <span className="font-medium text-slate-900">{item.planNombre}</span>
                    <span className="text-slate-500">{item.cantidadPagos} pago(s)</span>
                    <span className="font-semibold text-accent-600">
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
                    className="flex items-center justify-between rounded-xl border border-slate-200 px-4 py-2.5 text-sm transition-colors duration-200 hover:bg-slate-50"
                  >
                    <span className="font-medium text-slate-900">{item.negocioNombre}</span>
                    <span className="text-slate-500">{item.cantidadPagos} pago(s)</span>
                    <span className="font-semibold text-accent-600">
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
