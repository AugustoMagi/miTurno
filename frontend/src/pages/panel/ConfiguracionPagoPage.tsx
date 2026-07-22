import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import axios from 'axios'
import {
  conectarConfiguracionPago,
  desconectarConfiguracionPago,
  iniciarConexionMercadoPago,
  obtenerConfiguracionPago,
} from '../../api/configuracionPago'
import { extractError } from '../../api/client'
import { ProveedorPago } from '../../types/configuracionPago'
import type { ConfiguracionPago } from '../../types/configuracionPago'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'
import { FieldError } from '../../components/FieldError'
import { validarAlias } from '../../utils/validation'

export function ConfiguracionPagoPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()

  const [configuracion, setConfiguracion] = useState<ConfiguracionPago | null | undefined>(undefined)
  const [error, setError] = useState<string | null>(null)
  const [mensajeOAuth, setMensajeOAuth] = useState<{ tipo: 'ok' | 'error'; texto: string } | null>(null)
  const [conectandoConMercadoPago, setConectandoConMercadoPago] = useState(false)

  const [alias, setAlias] = useState('')
  const [guardando, setGuardando] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [aliasTocado, setAliasTocado] = useState(false)

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

  // Mercado Pago vuelve acá después del flujo OAuth (ver ConfiguracionesPagoController.CallbackMercadoPago)
  // con el resultado en la query string. Lo mostramos una vez y limpiamos la URL para que no reaparezca al recargar.
  useEffect(() => {
    const mp = searchParams.get('mp')
    if (!mp) return

    if (mp === 'conectado') {
      setMensajeOAuth({ tipo: 'ok', texto: 'Tu cuenta de Mercado Pago quedó conectada.' })
      cargar()
    } else if (mp === 'error') {
      setMensajeOAuth({
        tipo: 'error',
        texto: searchParams.get('mensaje') || 'No se pudo conectar con Mercado Pago.',
      })
    }
    navigate('/panel/configuracion-pago', { replace: true })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const errorAlias = validarAlias(alias)

  async function handleGuardarAlias(event: React.FormEvent) {
    event.preventDefault()
    setAliasTocado(true)
    if (errorAlias) return
    setGuardando(true)
    setFormError(null)
    try {
      await conectarConfiguracionPago({ proveedor: ProveedorPago.MercadoPago, alias })
      cargar()
    } catch (err) {
      setFormError(extractError(err))
    } finally {
      setGuardando(false)
    }
  }

  async function handleConectarMercadoPago() {
    setConectandoConMercadoPago(true)
    setError(null)
    try {
      const url = await iniciarConexionMercadoPago()
      window.location.href = url
    } catch (err) {
      setError(extractError(err))
      setConectandoConMercadoPago(false)
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

      {mensajeOAuth && mensajeOAuth.tipo === 'ok' && (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
          {mensajeOAuth.texto}
        </div>
      )}
      {mensajeOAuth && mensajeOAuth.tipo === 'error' && <ErrorBanner message={mensajeOAuth.texto} />}
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
              {configuracion.conectadoConOAuth
                ? 'Conectado con Mercado Pago'
                : configuracion.tieneAccessToken
                  ? 'Conectado (token manual)'
                  : 'No configurado'}
            </dd>
          </dl>
          {!configuracion.tieneAccessToken && (
            <p className="text-sm text-slate-500">
              Sin cobro automático, los clientes ven tu alias para transferir y vos confirmás el pago a mano
              desde Reservas.
            </p>
          )}
          <Button variant="secondary" disabled={desconectando} onClick={handleDesconectar} className="self-start">
            {desconectando ? 'Desconectando…' : 'Desconectar'}
          </Button>
        </Card>
      )}

      <Card className="flex flex-col gap-4">
        <div>
          <h2 className="font-semibold text-slate-900">Conectar con Mercado Pago</h2>
          <p className="mt-1 text-sm text-slate-500">
            Iniciá sesión en tu cuenta de Mercado Pago y autorizá a MiTurno. No necesitás copiar ni pegar
            ningún token: activa el cobro automático de tus reservas.
          </p>
        </div>
        <Button
          type="button"
          disabled={conectandoConMercadoPago}
          onClick={handleConectarMercadoPago}
          className="self-start"
        >
          {conectandoConMercadoPago ? 'Redirigiendo…' : 'Conectar con Mercado Pago'}
        </Button>
      </Card>

      <Card>
        <form className="flex flex-col gap-4" onSubmit={handleGuardarAlias}>
          <div>
            <h2 className="font-semibold text-slate-900">Alias o CVU para transferencia manual</h2>
            <p className="mt-1 text-sm text-slate-500">
              Si preferís no conectar Mercado Pago, cargá tu alias para que el cliente te transfiera y vos
              confirmes el pago a mano desde Reservas.
            </p>
          </div>
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
            Alias o CVU de Mercado Pago
            <input
              type="text"
              required
              maxLength={200}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
              value={alias}
              onChange={(event) => setAlias(event.target.value)}
              onBlur={() => setAliasTocado(true)}
            />
            {aliasTocado && <FieldError message={errorAlias} />}
          </label>
          {formError && <ErrorBanner message={formError} />}
          <Button type="submit" disabled={guardando} variant="secondary" className="self-start">
            {guardando ? 'Guardando…' : 'Guardar alias'}
          </Button>
        </form>
      </Card>
    </div>
  )
}
