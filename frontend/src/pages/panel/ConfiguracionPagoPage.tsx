import { useEffect, useState } from 'react'
import axios from 'axios'
import {
  conectarConfiguracionPago,
  desconectarConfiguracionPago,
  obtenerConfiguracionPago,
} from '../../api/configuracionPago'
import { extractError } from '../../api/client'
import { ProveedorPago } from '../../types/configuracionPago'
import type { ConfiguracionPago } from '../../types/configuracionPago'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'

export function ConfiguracionPagoPage() {
  const [configuracion, setConfiguracion] = useState<ConfiguracionPago | null | undefined>(undefined)
  const [error, setError] = useState<string | null>(null)

  const [alias, setAlias] = useState('')
  const [accessToken, setAccessToken] = useState('')
  const [guardando, setGuardando] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const [desconectando, setDesconectando] = useState(false)

  function cargar() {
    setError(null)
    obtenerConfiguracionPago()
      .then((data) => {
        setConfiguracion(data)
        setAlias(data.alias)
      })
      .catch((err) => {
        if (axios.isAxiosError(err) && err.response?.status === 404) {
          setConfiguracion(null)
          return
        }
        setError(extractError(err))
      })
  }

  useEffect(cargar, [])

  async function handleConectar(event: React.FormEvent) {
    event.preventDefault()
    setGuardando(true)
    setFormError(null)
    try {
      await conectarConfiguracionPago({
        proveedor: ProveedorPago.MercadoPago,
        alias,
        accessToken: accessToken || undefined,
      })
      setAccessToken('')
      cargar()
    } catch (err) {
      setFormError(extractError(err))
    } finally {
      setGuardando(false)
    }
  }

  async function handleDesconectar() {
    setDesconectando(true)
    setError(null)
    try {
      await desconectarConfiguracionPago()
      setConfiguracion(null)
      setAlias('')
    } catch (err) {
      setError(extractError(err))
    } finally {
      setDesconectando(false)
    }
  }

  if (configuracion === undefined) return <Spinner />

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold text-slate-900">Cobro</h1>

      {error && <ErrorBanner message={error} />}

      {configuracion?.activo && (
        <Card className="flex flex-col gap-3">
          <div className="flex items-center justify-between">
            <h2 className="font-semibold text-slate-900">Conectado</h2>
            <span className="rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-700">
              Activo
            </span>
          </div>
          <dl className="grid grid-cols-2 gap-y-2 text-sm">
            <dt className="text-slate-500">Alias / CVU</dt>
            <dd className="text-right font-medium text-slate-900">{configuracion.alias}</dd>
            <dt className="text-slate-500">Pago automático (Mercado Pago)</dt>
            <dd className="text-right font-medium text-slate-900">
              {configuracion.tieneAccessToken ? 'Conectado' : 'No configurado'}
            </dd>
          </dl>
          {!configuracion.tieneAccessToken && (
            <p className="text-sm text-slate-500">
              Sin Access Token, los clientes ven tu alias para transferir y vos confirmás el pago a mano
              desde Reservas.
            </p>
          )}
          <Button variant="secondary" disabled={desconectando} onClick={handleDesconectar} className="self-start">
            {desconectando ? 'Desconectando…' : 'Desconectar'}
          </Button>
        </Card>
      )}

      <Card>
        <form className="flex flex-col gap-4" onSubmit={handleConectar}>
          <h2 className="font-semibold text-slate-900">
            {configuracion?.activo ? 'Reemplazar configuración' : 'Conectar medio de cobro'}
          </h2>
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
            Alias o CVU de Mercado Pago
            <input
              type="text"
              required
              maxLength={200}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
              value={alias}
              onChange={(event) => setAlias(event.target.value)}
            />
          </label>
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
            Access Token de Mercado Pago (opcional, para cobro automático)
            <input
              type="password"
              maxLength={300}
              placeholder="Dejalo vacío para cobrar solo por transferencia manual"
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
              value={accessToken}
              onChange={(event) => setAccessToken(event.target.value)}
            />
          </label>
          {formError && <ErrorBanner message={formError} />}
          <Button type="submit" disabled={guardando} className="self-start">
            {guardando ? 'Guardando…' : 'Guardar'}
          </Button>
        </form>
      </Card>
    </div>
  )
}
