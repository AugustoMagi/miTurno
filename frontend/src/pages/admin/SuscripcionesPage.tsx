import { useEffect, useState } from 'react'
import { listarPlanes } from '../../api/planes'
import {
  cambiarPlanSuscripcion,
  cancelarSuscripcionAdmin,
  listarSuscripciones,
  renovarSuscripcion,
} from '../../api/suscripcionesAdmin'
import { extractError } from '../../api/client'
import type { Plan } from '../../types/plan'
import { EstadoSuscripcion } from '../../types/suscripcionAdmin'
import type { SuscripcionAdmin } from '../../types/suscripcionAdmin'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'
import { Field, Input, Select } from '../../components/Input'
import { validarFechaNoPasada } from '../../utils/validation'

function hoyIso(): string {
  const now = new Date()
  const offset = now.getTimezoneOffset()
  return new Date(now.getTime() - offset * 60_000).toISOString().slice(0, 10)
}

const ESTADO_LABEL: Record<EstadoSuscripcion, string> = {
  [EstadoSuscripcion.EnPrueba]: 'En prueba',
  [EstadoSuscripcion.Activa]: 'Activa',
  [EstadoSuscripcion.Vencida]: 'Vencida',
  [EstadoSuscripcion.Cancelada]: 'Cancelada',
}

const ESTADO_CLASSES: Record<EstadoSuscripcion, string> = {
  [EstadoSuscripcion.EnPrueba]: 'bg-amber-50 text-amber-700',
  [EstadoSuscripcion.Activa]: 'bg-emerald-50 text-emerald-700',
  [EstadoSuscripcion.Vencida]: 'bg-red-50 text-red-700',
  [EstadoSuscripcion.Cancelada]: 'bg-slate-100 text-slate-600',
}

interface FilaProps {
  suscripcion: SuscripcionAdmin
  planes: Plan[]
  onCambiada: () => void
}

function FilaSuscripcion({ suscripcion, planes, onCambiada }: FilaProps) {
  const [nuevoPlanId, setNuevoPlanId] = useState('')
  const [nuevoVencimiento, setNuevoVencimiento] = useState('')
  const [procesando, setProcesando] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleCambiarPlan() {
    if (!nuevoPlanId) return
    setProcesando(true)
    setError(null)
    try {
      await cambiarPlanSuscripcion(suscripcion.id, nuevoPlanId)
      onCambiada()
    } catch (err) {
      setError(extractError(err))
    } finally {
      setProcesando(false)
    }
  }

  const errorVencimiento = validarFechaNoPasada(nuevoVencimiento, hoyIso())

  async function handleRenovar() {
    if (!nuevoVencimiento || errorVencimiento) return
    setProcesando(true)
    setError(null)
    try {
      await renovarSuscripcion(suscripcion.id, nuevoVencimiento)
      onCambiada()
    } catch (err) {
      setError(extractError(err))
    } finally {
      setProcesando(false)
    }
  }

  async function handleCancelar() {
    setProcesando(true)
    setError(null)
    try {
      await cancelarSuscripcionAdmin(suscripcion.id)
      onCambiada()
    } catch (err) {
      setError(extractError(err))
    } finally {
      setProcesando(false)
    }
  }

  return (
    <Card hover className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <p className="font-semibold text-slate-900">{suscripcion.negocioNombre}</p>
          <p className="text-sm text-slate-500">{suscripcion.negocioEmail}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {suscripcion.cobroAutomaticoActivo && (
            <span className="rounded-full bg-link-50 px-2 py-0.5 text-xs font-medium text-link-700">
              Cobro automático
            </span>
          )}
          <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${ESTADO_CLASSES[suscripcion.estado]}`}>
            {ESTADO_LABEL[suscripcion.estado]}
          </span>
        </div>
      </div>
      <p className="text-sm text-slate-500">
        Plan <span className="font-medium text-slate-900">{suscripcion.planNombre}</span> · vence{' '}
        {suscripcion.fechaProximoVencimiento.slice(0, 10)}
      </p>

      {error && <ErrorBanner message={error} />}

      <div className="flex flex-wrap items-end gap-2 border-t border-slate-100 pt-3">
        <Field label="Cambiar de plan">
          <Select value={nuevoPlanId} onChange={(event) => setNuevoPlanId(event.target.value)} className="w-auto">
            <option value="">Elegir plan…</option>
            {planes.map((plan) => (
              <option key={plan.id} value={plan.id}>
                {plan.nombre}
              </option>
            ))}
          </Select>
        </Field>
        <Button variant="secondary" size="sm" disabled={!nuevoPlanId} loading={procesando} onClick={handleCambiarPlan}>
          Cambiar
        </Button>

        <Field label="Renovar hasta" error={nuevoVencimiento ? errorVencimiento : undefined}>
          <Input
            type="date"
            value={nuevoVencimiento}
            onChange={(event) => setNuevoVencimiento(event.target.value)}
            className="w-auto"
          />
        </Field>
        <Button
          variant="secondary"
          size="sm"
          disabled={!nuevoVencimiento || !!errorVencimiento}
          loading={procesando}
          onClick={handleRenovar}
        >
          Renovar
        </Button>

        {suscripcion.estado !== EstadoSuscripcion.Cancelada && (
          <Button variant="secondary" size="sm" loading={procesando} onClick={handleCancelar}>
            Cancelar
          </Button>
        )}
      </div>
    </Card>
  )
}

export function SuscripcionesPage() {
  const [suscripciones, setSuscripciones] = useState<SuscripcionAdmin[] | null>(null)
  const [planes, setPlanes] = useState<Plan[]>([])
  const [error, setError] = useState<string | null>(null)

  function cargar() {
    setError(null)
    Promise.all([listarSuscripciones(), listarPlanes()])
      .then(([suscripcionesData, planesData]) => {
        setSuscripciones(suscripcionesData)
        setPlanes(planesData.filter((p) => p.activo))
      })
      .catch((err) => setError(extractError(err)))
  }

  useEffect(cargar, [])

  if (error) return <ErrorBanner message={error} />
  if (!suscripciones) return <Spinner />

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold text-slate-900">Suscripciones</h1>

      {suscripciones.length === 0 ? (
        <p className="text-slate-500">Todavía no hay negocios con suscripción.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {suscripciones.map((suscripcion) => (
            <FilaSuscripcion key={suscripcion.id} suscripcion={suscripcion} planes={planes} onCambiada={cargar} />
          ))}
        </div>
      )}
    </div>
  )
}
