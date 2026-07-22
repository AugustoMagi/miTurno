import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import axios from 'axios'
import {
  cambiarPlanMiSuscripcion,
  cancelarMiSuscripcion,
  iniciarSuscripcionMercadoPago,
  obtenerMiSuscripcion,
} from '../../api/miSuscripcion'
import { listarPlanesPublicos } from '../../api/planesPublicos'
import { extractError } from '../../api/client'
import { Periodicidad } from '../../types/plan'
import { EstadoSuscripcion } from '../../types/suscripcionAdmin'
import type { MiSuscripcion } from '../../types/miSuscripcion'
import type { PlanPublico } from '../../types/planPublico'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'

const ESTADO_LABEL: Record<EstadoSuscripcion, string> = {
  [EstadoSuscripcion.EnPrueba]: 'En prueba',
  [EstadoSuscripcion.Activa]: 'Activa',
  [EstadoSuscripcion.Vencida]: 'Vencida',
  [EstadoSuscripcion.Cancelada]: 'Cancelada',
}

const PERIODICIDAD_LABEL: Record<Periodicidad, string> = {
  [Periodicidad.Mensual]: 'mensual',
  [Periodicidad.Anual]: 'anual',
}

function diasRestantes(fechaProximoVencimiento: string): number {
  const ms = new Date(fechaProximoVencimiento).getTime() - Date.now()
  return Math.ceil(ms / (1000 * 60 * 60 * 24))
}

function TextoVencimiento({ suscripcion }: { suscripcion: MiSuscripcion }) {
  const dias = diasRestantes(suscripcion.fechaProximoVencimiento)
  const fecha = suscripcion.fechaProximoVencimiento.slice(0, 10)

  if (suscripcion.estado === EstadoSuscripcion.Cancelada) {
    return <p className="text-sm text-slate-500">Vencía el {fecha}.</p>
  }
  if (dias < 0) {
    return <p className="text-sm text-red-600">Venció el {fecha}.</p>
  }
  if (dias === 0) {
    return <p className="text-sm text-amber-600">Vence hoy ({fecha}).</p>
  }
  return (
    <p className="text-sm text-slate-500">
      Vence en {dias} día{dias === 1 ? '' : 's'} ({fecha}).
    </p>
  )
}

export function MiSuscripcionPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()

  const [suscripcion, setSuscripcion] = useState<MiSuscripcion | null | undefined>(undefined)
  const [planes, setPlanes] = useState<PlanPublico[]>([])
  const [error, setError] = useState<string | null>(null)
  const [vuelvoDeMercadoPago, setVuelvoDeMercadoPago] = useState(false)

  const [suscribiendo, setSuscribiendo] = useState(false)

  const [nuevoPlanId, setNuevoPlanId] = useState('')
  const [cambiandoPlan, setCambiandoPlan] = useState(false)
  const [cancelando, setCancelando] = useState(false)

  function cargar() {
    setError(null)
    obtenerMiSuscripcion()
      .then((data) => {
        setSuscripcion(data)
        setNuevoPlanId(data.planId)
      })
      .catch((err) => {
        if (axios.isAxiosError(err) && err.response?.status === 404) {
          setSuscripcion(null)
          return
        }
        setError(extractError(err))
      })
    listarPlanesPublicos()
      .then(setPlanes)
      .catch(() => {})
  }

  useEffect(cargar, [])

  // Mercado Pago vuelve acá después de que el negocio autoriza (o no) el cobro recurrente. La
  // activación llega después, por webhook, así que puede tardar en reflejarse.
  useEffect(() => {
    if (searchParams.get('mp') === 'vuelta') {
      setVuelvoDeMercadoPago(true)
      navigate('/panel/suscripcion', { replace: true })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function handleSuscribirme() {
    setSuscribiendo(true)
    setError(null)
    try {
      const initPoint = await iniciarSuscripcionMercadoPago()
      window.location.href = initPoint
    } catch (err) {
      setError(extractError(err))
      setSuscribiendo(false)
    }
  }

  async function handleCambiarPlan() {
    if (!nuevoPlanId) return
    setCambiandoPlan(true)
    setError(null)
    try {
      const actualizada = await cambiarPlanMiSuscripcion(nuevoPlanId)
      setSuscripcion(actualizada)
    } catch (err) {
      setError(extractError(err))
    } finally {
      setCambiandoPlan(false)
    }
  }

  async function handleCancelar() {
    if (!window.confirm('¿Seguro que querés cancelar tu suscripción? Perdés acceso al finalizar el período vigente.')) {
      return
    }
    setCancelando(true)
    setError(null)
    try {
      await cancelarMiSuscripcion()
      cargar()
    } catch (err) {
      setError(extractError(err))
    } finally {
      setCancelando(false)
    }
  }

  if (suscripcion === undefined) return <Spinner />

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold text-slate-900">Mi suscripción</h1>

      {vuelvoDeMercadoPago && (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
          Estamos confirmando tu suscripción con Mercado Pago. Puede tardar unos minutos en reflejarse acá.
        </div>
      )}
      {error && <ErrorBanner message={error} />}

      {suscripcion === null ? (
        <Card>
          <p className="text-slate-500">Todavía no tenés una suscripción asignada.</p>
        </Card>
      ) : (
        <>
          <Card className="flex flex-col gap-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="font-semibold text-slate-900">{suscripcion.planNombre}</p>
                <p className="text-sm text-slate-500">
                  ${suscripcion.planPrecio.toLocaleString('es-AR')} / {PERIODICIDAD_LABEL[suscripcion.periodicidad]}
                </p>
              </div>
              <span
                className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                  suscripcion.estaActiva ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-700'
                }`}
              >
                {ESTADO_LABEL[suscripcion.estado]}
              </span>
            </div>
            <TextoVencimiento suscripcion={suscripcion} />

            {suscripcion.cobroAutomaticoActivo ? (
              <p className="text-sm text-emerald-700">
                ✓ Cobro automático activado: Mercado Pago te cobra solo en cada renovación.
              </p>
            ) : suscripcion.estado === EstadoSuscripcion.Cancelada ? null : (
              <Button disabled={suscribiendo} onClick={handleSuscribirme} className="self-start">
                {suscribiendo ? 'Redirigiendo…' : 'Suscribirme con Mercado Pago'}
              </Button>
            )}
          </Card>

          {planes.length > 0 && (
            <Card className="flex flex-col gap-3">
              <h2 className="font-semibold text-slate-900">Cambiar de plan</h2>
              <div className="flex flex-wrap items-end gap-2">
                <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
                  Nuevo plan
                  <select
                    className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                    value={nuevoPlanId}
                    onChange={(event) => setNuevoPlanId(event.target.value)}
                  >
                    {planes.map((plan) => (
                      <option key={plan.id} value={plan.id}>
                        {plan.nombre} · ${plan.precio.toLocaleString('es-AR')}
                      </option>
                    ))}
                  </select>
                </label>
                <Button
                  variant="secondary"
                  disabled={cambiandoPlan || !nuevoPlanId || nuevoPlanId === suscripcion.planId}
                  onClick={handleCambiarPlan}
                >
                  {cambiandoPlan ? 'Cambiando…' : 'Cambiar plan'}
                </Button>
              </div>
            </Card>
          )}

          {suscripcion.estado !== EstadoSuscripcion.Cancelada && (
            <Card className="flex flex-col gap-3">
              <h2 className="font-semibold text-slate-900">Cancelar suscripción</h2>
              <p className="text-sm text-slate-500">
                {suscripcion.cobroAutomaticoActivo
                  ? 'También cancela el cobro automático en Mercado Pago: no te va a cobrar de nuevo.'
                  : 'Dejás de tener acceso al finalizar el período vigente.'}{' '}
                Podés volver a suscribirte cuando quieras.
              </p>
              <button
                type="button"
                disabled={cancelando}
                onClick={handleCancelar}
                className="self-start rounded-lg border border-red-300 px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50 disabled:opacity-50"
              >
                {cancelando ? 'Cancelando…' : 'Cancelar suscripción'}
              </button>
            </Card>
          )}
        </>
      )}
    </div>
  )
}
