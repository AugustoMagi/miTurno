import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import axios from 'axios'
import {
  cambiarPlanMiSuscripcion,
  cancelarMiSuscripcion,
  elegirPlan,
  iniciarSuscripcionMercadoPago,
  obtenerMiSuscripcion,
  reanudarCobroAutomatico,
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
import { CheckIcon, XIcon } from '../../components/icons'

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

function TextoCobroAutomatico({
  suscripcion,
  reanudando,
  reactivando,
  onReanudar,
  onReactivar,
}: {
  suscripcion: MiSuscripcion
  reanudando: boolean
  reactivando: boolean
  onReanudar: () => void
  onReactivar: () => void
}) {
  const fecha = suscripcion.fechaProximoVencimiento.slice(0, 10)

  if (suscripcion.cobroAutomaticoActivo) {
    return (
      <p className="flex items-center gap-1.5 text-sm font-medium text-emerald-700">
        <CheckIcon className="h-4 w-4" />
        Cobro automático activo: se renueva solo el {fecha}.
      </p>
    )
  }

  // Pausado: la Preapproval de Mercado Pago sigue autorizada, así que reanudar es instantáneo (un
  // PUT nuestro), sin volver a pasar por el checkout de MP.
  if (suscripcion.cobroAutomaticoPausado) {
    return (
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="flex items-center gap-1.5 text-sm font-medium text-slate-500">
          <XIcon className="h-4 w-4" />
          Cobro automático pausado: no se te va a cobrar hasta que lo reactives.
        </p>
        <Button variant="secondary" size="sm" loading={reanudando} onClick={onReanudar}>
          Reactivar cobro automático
        </Button>
      </div>
    )
  }

  // Nunca se activó (o quedó cancelado del todo en Mercado Pago): no hay nada que reanudar, hay que
  // autorizar una Preapproval nueva desde el checkout de MP.
  return (
    <div className="flex flex-wrap items-center justify-between gap-2">
      <p className="flex items-center gap-1.5 text-sm font-medium text-slate-500">
        <XIcon className="h-4 w-4" />
        Cobro automático desactivado: no se te va a volver a cobrar.
      </p>
      <Button variant="secondary" size="sm" loading={reactivando} onClick={onReactivar}>
        Activar cobro automático
      </Button>
    </div>
  )
}

function PlanCard({
  plan,
  esElActual,
  cobroAutomaticoActivo,
  cobroAutomaticoPausado,
  procesando,
  onSeleccionar,
}: {
  plan: PlanPublico
  esElActual: boolean
  cobroAutomaticoActivo: boolean
  cobroAutomaticoPausado: boolean
  procesando: boolean
  onSeleccionar: () => void
}) {
  return (
    <div
      className={`flex h-full flex-col gap-3 rounded-xl border bg-white p-6 shadow-soft transition-all duration-200 hover:-translate-y-1 hover:shadow-soft-lg ${
        esElActual ? 'border-accent-300 ring-1 ring-accent-100' : 'border-slate-200'
      }`}
    >
      <div className="flex items-center gap-2">
        <h3 className="font-semibold text-slate-900">{plan.nombre}</h3>
        {esElActual && (
          <span className="rounded-full bg-accent-50 px-2 py-0.5 text-xs font-medium text-accent-700">
            Tu plan actual
          </span>
        )}
      </div>
      <p className="text-slate-900">
        <span className="text-2xl font-bold" style={{ fontFamily: 'var(--font-heading)' }}>
          ${plan.precio.toLocaleString('es-AR')}
        </span>
        <span className="text-sm text-slate-500"> / {PERIODICIDAD_LABEL[plan.periodicidad]}</span>
      </p>
      <ul className="flex flex-1 flex-col gap-1 text-sm text-slate-600">
        <li>Hasta {plan.limiteRecursos} cancha{plan.limiteRecursos === 1 ? '' : 's'}</li>
        <li>{plan.limiteReservasPorMes} reservas por mes</li>
      </ul>

      {esElActual && cobroAutomaticoActivo ? (
        <p className="flex items-center gap-1.5 text-sm font-medium text-emerald-700">
          <CheckIcon className="h-4 w-4" />
          Cobro automático activo
        </p>
      ) : esElActual && cobroAutomaticoPausado ? (
        <p className="text-sm text-slate-500">Cobro automático pausado — reactivalo arriba.</p>
      ) : (
        <Button loading={procesando} onClick={onSeleccionar} className="mt-auto">
          {esElActual ? 'Suscribirme con Mercado Pago' : 'Cambiar a este plan'}
        </Button>
      )}
    </div>
  )
}

export function MiSuscripcionPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()

  const [suscripcion, setSuscripcion] = useState<MiSuscripcion | null | undefined>(undefined)
  const [planes, setPlanes] = useState<PlanPublico[]>([])
  const [error, setError] = useState<string | null>(null)
  const [vuelvoDeMercadoPago, setVuelvoDeMercadoPago] = useState(false)

  const [procesandoPlanId, setProcesandoPlanId] = useState<string | null>(null)
  const [cancelando, setCancelando] = useState(false)
  const [reactivando, setReactivando] = useState(false)
  const [reanudando, setReanudando] = useState(false)

  function cargarSuscripcion() {
    return obtenerMiSuscripcion()
      .then(setSuscripcion)
      .catch((err) => {
        if (axios.isAxiosError(err) && err.response?.status === 404) {
          setSuscripcion(null)
          return
        }
        setError(extractError(err))
      })
  }

  function cargar() {
    setError(null)
    cargarSuscripcion()
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

  // Mientras esperamos que el webhook confirme el primer cobro, la suscripción sigue mostrando el
  // estado viejo (ej. "Cancelada" si se venía de cancelar y volver a suscribir). Sondeamos unas
  // cuantas veces hasta ver que el estado cambió, en vez de dejar la card desactualizada para siempre.
  useEffect(() => {
    if (!vuelvoDeMercadoPago) return

    let cancelado = false
    let intentos = 0

    async function sondear() {
      intentos += 1
      try {
        const actual = await obtenerMiSuscripcion()
        if (cancelado) return
        setSuscripcion(actual)
        if (actual.estado !== EstadoSuscripcion.Cancelada) {
          setVuelvoDeMercadoPago(false)
          return
        }
      } catch {
        // error transitorio: seguimos sondeando, no cortamos por esto
      }
      if (!cancelado && intentos < 15) {
        setTimeout(sondear, 4000)
      }
    }

    const timeoutId = setTimeout(sondear, 4000)
    return () => {
      cancelado = true
      clearTimeout(timeoutId)
    }
  }, [vuelvoDeMercadoPago])

  // Elige/cambia el plan (según si ya había una suscripción asignada) y, si el plan tiene costo y
  // todavía no hay cobro automático activo para él, manda directo a pagarlo con Mercado Pago: un
  // solo click cubre "elegir/cambiar" y "pagar", en vez de dos pasos separados. Si es un cambio de
  // plan (no el mismo de siempre), se cobra ya mismo en vez de esperar al vencimiento del período
  // viejo: el negocio decidió pagar el nuevo precio ahora.
  async function handleSeleccionarPlan(plan: PlanPublico) {
    const esCambioDePlan = suscripcion !== null && suscripcion.planId !== plan.id
    setProcesandoPlanId(plan.id)
    setError(null)
    try {
      let actual: MiSuscripcion | null = suscripcion ?? null
      if (actual === null) {
        actual = await elegirPlan(plan.id)
      } else if (esCambioDePlan) {
        actual = await cambiarPlanMiSuscripcion(plan.id)
      }
      setSuscripcion(actual)

      if (plan.precio > 0 && !actual.cobroAutomaticoActivo) {
        const initPoint = await iniciarSuscripcionMercadoPago(esCambioDePlan)
        window.location.href = initPoint
        return
      }
    } catch (err) {
      setError(extractError(err))
    } finally {
      setProcesandoPlanId(null)
    }
  }

  // Reanuda un cobro automático pausado: reusa la Preapproval ya autorizada en Mercado Pago, así que
  // no hace falta mandar al negocio de nuevo al checkout de MP.
  async function handleReanudarCobroAutomatico() {
    setReanudando(true)
    setError(null)
    try {
      await reanudarCobroAutomatico()
      cargar()
    } catch (err) {
      setError(extractError(err))
    } finally {
      setReanudando(false)
    }
  }

  // Sólo para cuando NO hay una Preapproval pausada para reanudar (nunca se activó, o quedó
  // cancelada del todo en Mercado Pago): acá sí hace falta autorizar una nueva desde el checkout.
  async function handleReactivarCobroAutomatico() {
    setReactivando(true)
    setError(null)
    try {
      const initPoint = await iniciarSuscripcionMercadoPago()
      window.location.href = initPoint
    } catch (err) {
      setError(extractError(err))
      setReactivando(false)
    }
  }

  async function handleCancelar() {
    const fecha = suscripcion?.fechaProximoVencimiento.slice(0, 10)
    if (
      !window.confirm(
        `¿Seguro que querés cancelar el cobro automático? No se te va a volver a cobrar, pero conservás el acceso hasta el ${fecha}.`,
      )
    ) {
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
        <div className="rounded-xl border border-link-200 bg-link-50 px-4 py-3 text-sm text-link-700">
          Estamos confirmando tu suscripción con Mercado Pago. Puede tardar unos minutos en reflejarse acá.
        </div>
      )}
      {error && <ErrorBanner message={error} />}

      {suscripcion !== null && (
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
          {suscripcion.planPrecio > 0 && (
            <TextoCobroAutomatico
              suscripcion={suscripcion}
              reanudando={reanudando}
              reactivando={reactivando}
              onReanudar={handleReanudarCobroAutomatico}
              onReactivar={handleReactivarCobroAutomatico}
            />
          )}
        </Card>
      )}

      {planes.length > 0 && (
        <div className="flex flex-col gap-3">
          {suscripcion === null && <p className="text-slate-500">Todavía no tenés una suscripción asignada — elegí un plan.</p>}
          <p className="text-xs text-slate-400">
            Los pagos con Mercado Pago pueden tardar unos minutos en reflejarse acá — no hace falta
            que vuelvas a pagar si ya lo hiciste.
          </p>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {planes.map((plan) => (
              <PlanCard
                key={plan.id}
                plan={plan}
                esElActual={suscripcion !== null && suscripcion.planId === plan.id}
                cobroAutomaticoActivo={
                  suscripcion !== null && suscripcion.planId === plan.id && suscripcion.cobroAutomaticoActivo
                }
                cobroAutomaticoPausado={
                  suscripcion !== null && suscripcion.planId === plan.id && suscripcion.cobroAutomaticoPausado
                }
                procesando={procesandoPlanId === plan.id}
                onSeleccionar={() => handleSeleccionarPlan(plan)}
              />
            ))}
          </div>
        </div>
      )}

      {suscripcion !== null && (suscripcion.estado !== EstadoSuscripcion.Cancelada || suscripcion.cobroAutomaticoActivo) && (
        <Card className="flex flex-col gap-3">
          <h2 className="font-semibold text-slate-900">Cancelar cobro automático</h2>
          <p className="text-sm text-slate-500">
            Mercado Pago no te va a volver a cobrar, pero seguís teniendo acceso al plan hasta el{' '}
            {suscripcion.fechaProximoVencimiento.slice(0, 10)}. Podés volver a suscribirte cuando quieras.
          </p>
          <Button
            variant="secondary"
            loading={cancelando}
            onClick={handleCancelar}
            className="self-start border-red-300 text-red-600 hover:bg-red-50"
          >
            Cancelar cobro automático
          </Button>
        </Card>
      )}
    </div>
  )
}
