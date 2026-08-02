import { useEffect, useState } from 'react'
import axios from 'axios'
import {
  cambiarPlanConPago,
  cambiarPlanMiSuscripcion,
  cancelarMiSuscripcion,
  elegirPlan,
  iniciarSuscripcionMercadoPago,
  obtenerMiSuscripcion,
  reanudarCobroAutomatico,
} from '../../api/miSuscripcion'
import { listarPlanesPublicos } from '../../api/planesPublicos'
import { listarRecursos } from '../../api/recursos'
import { extractError } from '../../api/client'
import { Periodicidad } from '../../types/plan'
import { EstadoSuscripcion } from '../../types/suscripcionAdmin'
import type { MiSuscripcion } from '../../types/miSuscripcion'
import type { PlanPublico } from '../../types/planPublico'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'
import { CheckIcon, LayersIcon, XIcon } from '../../components/icons'

// Se guarda antes de cada redirect a Mercado Pago y se chequea al volver — no importa cómo: que MP
// redirija de nuevo a nuestra back_url, que el negocio apriete "atrás" en el navegador (incluso si el
// browser restaura la página desde bfcache sin recargarla), o que cierre y reabra la pestaña. El
// query param ?mp=vuelta que usábamos antes sólo cubría el primer caso.
const MP_CHECKOUT_PENDIENTE_KEY = 'miturno_mp_checkout_pendiente'

function irAMercadoPago(initPoint: string) {
  sessionStorage.setItem(MP_CHECKOUT_PENDIENTE_KEY, '1')
  window.location.href = initPoint
}

const ESTADO_LABEL: Record<EstadoSuscripcion, string> = {
  [EstadoSuscripcion.EnPrueba]: 'En prueba',
  [EstadoSuscripcion.Activa]: 'Activa',
  [EstadoSuscripcion.Vencida]: 'Vencida',
  [EstadoSuscripcion.Cancelada]: 'Cancelada',
}

// Cancelada sólo apaga la renovación automática — mientras estaActiva sea true, el negocio sigue
// con acceso pleno, así que mostrarle literalmente "Cancelada" es engañoso (parece que perdió todo).
function estadoVisibleLabel(suscripcion: MiSuscripcion): string {
  if (!suscripcion.estaActiva) return ESTADO_LABEL[EstadoSuscripcion.Vencida]
  if (suscripcion.estado === EstadoSuscripcion.EnPrueba) return ESTADO_LABEL[EstadoSuscripcion.EnPrueba]
  return ESTADO_LABEL[EstadoSuscripcion.Activa]
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
  recursosActivos,
  procesando,
  textoBoton,
  onSeleccionar,
}: {
  plan: PlanPublico
  recursosActivos: number
  procesando: boolean
  textoBoton: string
  onSeleccionar: () => void
}) {
  const excedeLimite = recursosActivos > plan.limiteRecursos

  return (
    <div className="flex h-full flex-col gap-3 rounded-xl border border-slate-200 bg-white p-6 shadow-soft transition-all duration-200 hover:-translate-y-1 hover:shadow-soft-lg">
      <h3 className="font-semibold text-slate-900">{plan.nombre}</h3>
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

      {excedeLimite ? (
        <p className="text-sm text-red-600">
          Tenés {recursosActivos} cancha{recursosActivos === 1 ? '' : 's'} activa
          {recursosActivos === 1 ? '' : 's'} y este plan permite hasta {plan.limiteRecursos}. Desactivá
          canchas antes de cambiar.
        </p>
      ) : (
        <Button loading={procesando} onClick={onSeleccionar} className="mt-auto">
          {textoBoton}
        </Button>
      )}
    </div>
  )
}

export function MiSuscripcionPage() {
  const [suscripcion, setSuscripcion] = useState<MiSuscripcion | null | undefined>(undefined)
  const [planes, setPlanes] = useState<PlanPublico[]>([])
  const [recursosActivos, setRecursosActivos] = useState(0)
  const [mostrandoCambioPlan, setMostrandoCambioPlan] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [resultadoMercadoPago, setResultadoMercadoPago] = useState<
    'pendiente' | 'confirmado' | 'no-detectado' | null
  >(null)

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
    listarRecursos()
      .then((recursos) => setRecursosActivos(recursos.filter((r) => r.activo).length))
      .catch(() => {})
  }

  useEffect(cargar, [])

  // Sondea si el pago con Mercado Pago se confirmó o no. Se dispara al volver de un redirect a MP
  // (ver irAMercadoPago): CobroAutomaticoActivo prendiéndose es la única señal confiable de que el
  // pago se efectuó — si se agotan los intentos sin que eso pase, lo más probable es que el negocio
  // haya cerrado/vuelto atrás del checkout sin llegar a autorizar nada (Mercado Pago deja la
  // Preapproval en "pending" para siempre en ese caso, así que seguir esperando no cambiaría nada).
  useEffect(() => {
    function chequearVueltaDeMercadoPago() {
      if (sessionStorage.getItem(MP_CHECKOUT_PENDIENTE_KEY) === '1') {
        setResultadoMercadoPago('pendiente')
      }
    }

    chequearVueltaDeMercadoPago()
    // 'pageshow' cubre volver con el botón atrás del navegador: el browser puede restaurar la página
    // desde bfcache sin volver a montar el componente, así que el chequeo de arriba solo no alcanza.
    window.addEventListener('pageshow', chequearVueltaDeMercadoPago)
    return () => window.removeEventListener('pageshow', chequearVueltaDeMercadoPago)
  }, [])

  useEffect(() => {
    if (resultadoMercadoPago !== 'pendiente') return

    let cancelado = false
    let intentos = 0

    async function sondear() {
      intentos += 1
      try {
        const actual = await obtenerMiSuscripcion()
        if (cancelado) return
        setSuscripcion(actual)
        if (actual.cobroAutomaticoActivo) {
          sessionStorage.removeItem(MP_CHECKOUT_PENDIENTE_KEY)
          setResultadoMercadoPago('confirmado')
          // Refresca también los planes y las canchas activas, no sólo la suscripción: confirmado el
          // pago puede cambiar qué opciones de "Cambiar plan" corresponden mostrar.
          cargar()
          return
        }
      } catch {
        // error transitorio: seguimos sondeando, no cortamos por esto
      }
      if (cancelado) return
      if (intentos < 15) {
        setTimeout(sondear, 4000)
      } else {
        sessionStorage.removeItem(MP_CHECKOUT_PENDIENTE_KEY)
        setResultadoMercadoPago('no-detectado')
      }
    }

    const timeoutId = setTimeout(sondear, 4000)
    return () => {
      cancelado = true
      clearTimeout(timeoutId)
    }
  }, [resultadoMercadoPago])

  // Primera elección de plan (todavía sin suscripción asignada): arranca la prueba gratis de ese
  // plan y, si tiene costo, manda a autorizar el cobro automático — pero recién cobra cuando termine
  // la prueba (cobrarInmediato en false), no el día que se autoriza.
  async function handleElegirPlan(plan: PlanPublico) {
    setProcesandoPlanId(plan.id)
    setError(null)
    try {
      const actual = await elegirPlan(plan.id)
      setSuscripcion(actual)

      if (plan.precio > 0 && !actual.cobroAutomaticoActivo) {
        const initPoint = await iniciarSuscripcionMercadoPago()
        irAMercadoPago(initPoint)
        return
      }
    } catch (err) {
      setError(extractError(err))
    } finally {
      setProcesandoPlanId(null)
    }
  }

  // Cambio a un plan distinto del que ya tenía asignado: siempre manda a pagar a Mercado Pago (si el
  // plan tiene costo), sin importar si el cobro automático del plan viejo ya estaba activo — es un
  // compromiso nuevo, a un precio nuevo, así que se re-autoriza de cero en vez de dejarlo cobrando en
  // silencio el monto viejo hasta la próxima renovación.
  async function handleCambiarAPlan(plan: PlanPublico) {
    setProcesandoPlanId(plan.id)
    setError(null)
    try {
      if (plan.precio > 0) {
        // Ojo: esto NO cambia el plan todavía — crea la Preapproval del plan nuevo sin tocar el plan
        // ni el cobro automático vigentes. Si no llegás a pagar (cerrás MP, volvés atrás), seguís con
        // el plan de antes; recién se confirma cuando el pago se autoriza de verdad.
        const initPoint = await cambiarPlanConPago(plan.id)
        irAMercadoPago(initPoint)
        return
      }
      // Plan sin costo: no hay nada que pagar, se cambia directo.
      await cambiarPlanMiSuscripcion(plan.id)
      cargar()
      setMostrandoCambioPlan(false)
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
      irAMercadoPago(initPoint)
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

      {resultadoMercadoPago === 'pendiente' && (
        <div className="rounded-xl border border-link-200 bg-link-50 px-4 py-3 text-sm text-link-700">
          Estamos confirmando tu pago con Mercado Pago. Puede tardar unos minutos en reflejarse acá.
        </div>
      )}
      {resultadoMercadoPago === 'confirmado' && (
        <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
          ¡Listo! Confirmamos tu pago y el cobro automático quedó activado.
        </div>
      )}
      {resultadoMercadoPago === 'no-detectado' && (
        <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-700">
          No detectamos que hayas completado el pago en Mercado Pago. Si no llegaste a pagar, no pasa
          nada — podés intentarlo de nuevo cuando quieras. Si ya pagaste, puede tardar unos minutos más
          en reflejarse acá.
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
              {estadoVisibleLabel(suscripcion)}
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

      {/* Onboarding: todavía sin suscripción asignada, se elige el primer plan. */}
      {suscripcion === null && planes.length > 0 && (
        <div className="flex flex-col gap-3">
          <p className="text-slate-500">Todavía no tenés una suscripción asignada — elegí un plan.</p>
          <p className="text-xs text-slate-400">
            Los pagos con Mercado Pago pueden tardar unos minutos en reflejarse acá — no hace falta
            que vuelvas a pagar si ya lo hiciste.
          </p>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {planes.map((plan) => (
              <PlanCard
                key={plan.id}
                plan={plan}
                recursosActivos={recursosActivos}
                procesando={procesandoPlanId === plan.id}
                textoBoton="Elegir este plan"
                onSeleccionar={() => handleElegirPlan(plan)}
              />
            ))}
          </div>
        </div>
      )}

      {/* Ya con suscripción asignada: cambiar de plan es una acción aparte, detrás de un botón —
          el plan actual no aparece en la lista (no tiene sentido "cambiar" al mismo de siempre; para
          eso está la sección de cobro automático de arriba). */}
      {suscripcion !== null && planes.length > 0 && (
        <Card className="flex flex-col gap-3">
          <h2 className="font-semibold text-slate-900">Cambiar de plan</h2>
          <p className="text-sm text-slate-500">
            ¿Necesitás más (o menos) canchas o reservas por mes? Elegí otro plan — te vamos a llevar a
            Mercado Pago para confirmar el pago al precio nuevo.
          </p>
          <Button
            variant="secondary"
            icon={<LayersIcon />}
            className="self-start"
            onClick={() => setMostrandoCambioPlan((v) => !v)}
          >
            {mostrandoCambioPlan ? 'Ocultar planes' : 'Ver planes disponibles'}
          </Button>

          {mostrandoCambioPlan && (
            <div className="grid grid-cols-1 gap-4 pt-2 sm:grid-cols-2 lg:grid-cols-3">
              {planes
                .filter((plan) => plan.id !== suscripcion.planId)
                .map((plan) => (
                  <PlanCard
                    key={plan.id}
                    plan={plan}
                    recursosActivos={recursosActivos}
                    procesando={procesandoPlanId === plan.id}
                    textoBoton="Cambiar a este plan"
                    onSeleccionar={() => handleCambiarAPlan(plan)}
                  />
                ))}
            </div>
          )}
        </Card>
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
