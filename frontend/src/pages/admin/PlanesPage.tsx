import { useEffect, useState } from 'react'
import { actualizarPlan, crearPlan, desactivarPlan, listarPlanes, marcarPlanDePrueba } from '../../api/planes'
import { extractError } from '../../api/client'
import { Periodicidad } from '../../types/plan'
import type { Plan, PlanInput } from '../../types/plan'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'

const PERIODICIDAD_LABEL: Record<Periodicidad, string> = {
  [Periodicidad.Mensual]: 'Mensual',
  [Periodicidad.Anual]: 'Anual',
}

const FORM_VACIO: PlanInput = {
  nombre: '',
  precio: 0,
  periodicidad: Periodicidad.Mensual,
  limiteRecursos: 1,
  limiteReservasPorMes: 100,
}

export function PlanesPage() {
  const [planes, setPlanes] = useState<Plan[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const [mostrarForm, setMostrarForm] = useState(false)
  const [editandoId, setEditandoId] = useState<string | null>(null)
  const [form, setForm] = useState<PlanInput>(FORM_VACIO)
  const [guardando, setGuardando] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const [procesando, setProcesando] = useState<string | null>(null)

  function cargar() {
    setError(null)
    listarPlanes()
      .then(setPlanes)
      .catch((err) => setError(extractError(err)))
  }

  useEffect(cargar, [])

  function abrirNuevo() {
    setEditandoId(null)
    setForm(FORM_VACIO)
    setMostrarForm(true)
  }

  function abrirEdicion(plan: Plan) {
    setEditandoId(plan.id)
    setForm({
      nombre: plan.nombre,
      precio: plan.precio,
      periodicidad: plan.periodicidad,
      limiteRecursos: plan.limiteRecursos,
      limiteReservasPorMes: plan.limiteReservasPorMes,
    })
    setMostrarForm(true)
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setGuardando(true)
    setFormError(null)
    try {
      if (editandoId) await actualizarPlan(editandoId, form)
      else await crearPlan(form)
      setMostrarForm(false)
      cargar()
    } catch (err) {
      setFormError(extractError(err))
    } finally {
      setGuardando(false)
    }
  }

  async function handleDesactivar(id: string) {
    setProcesando(id)
    setError(null)
    try {
      await desactivarPlan(id)
      cargar()
    } catch (err) {
      setError(extractError(err))
    } finally {
      setProcesando(null)
    }
  }

  async function handleMarcarDePrueba(id: string) {
    setProcesando(id)
    setError(null)
    try {
      await marcarPlanDePrueba(id)
      cargar()
    } catch (err) {
      setError(extractError(err))
    } finally {
      setProcesando(null)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-slate-900">Planes</h1>
        <Button onClick={mostrarForm && !editandoId ? () => setMostrarForm(false) : abrirNuevo}>
          {mostrarForm && !editandoId ? 'Cancelar' : 'Nuevo plan'}
        </Button>
      </div>

      {mostrarForm && (
        <Card>
          <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
            <h2 className="font-semibold text-slate-900">{editandoId ? 'Editar plan' : 'Nuevo plan'}</h2>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
                Nombre
                <input
                  type="text"
                  required
                  maxLength={100}
                  className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                  value={form.nombre}
                  onChange={(event) => setForm({ ...form, nombre: event.target.value })}
                />
              </label>
              <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
                Precio
                <input
                  type="number"
                  required
                  min={0}
                  step="0.01"
                  className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                  value={form.precio}
                  onChange={(event) => setForm({ ...form, precio: Number(event.target.value) })}
                />
              </label>
              <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
                Periodicidad
                <select
                  className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                  value={form.periodicidad}
                  onChange={(event) => setForm({ ...form, periodicidad: Number(event.target.value) as Periodicidad })}
                >
                  <option value={Periodicidad.Mensual}>Mensual</option>
                  <option value={Periodicidad.Anual}>Anual</option>
                </select>
              </label>
              <div />
              <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
                Límite de recursos
                <input
                  type="number"
                  required
                  min={1}
                  className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                  value={form.limiteRecursos}
                  onChange={(event) => setForm({ ...form, limiteRecursos: Number(event.target.value) })}
                />
              </label>
              <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
                Límite de reservas por mes
                <input
                  type="number"
                  required
                  min={1}
                  className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                  value={form.limiteReservasPorMes}
                  onChange={(event) => setForm({ ...form, limiteReservasPorMes: Number(event.target.value) })}
                />
              </label>
            </div>
            {formError && <ErrorBanner message={formError} />}
            <Button type="submit" disabled={guardando} className="self-start">
              {guardando ? 'Guardando…' : editandoId ? 'Guardar cambios' : 'Crear plan'}
            </Button>
          </form>
        </Card>
      )}

      {error && <ErrorBanner message={error} />}

      {!planes ? (
        <Spinner />
      ) : planes.length === 0 ? (
        <p className="text-slate-500">Todavía no hay planes cargados.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {planes.map((plan) => (
            <Card key={plan.id} className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-semibold text-slate-900">{plan.nombre}</span>
                  <span
                    className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                      plan.activo ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'
                    }`}
                  >
                    {plan.activo ? 'Activo' : 'Inactivo'}
                  </span>
                  {plan.esPlanDePrueba && (
                    <span className="rounded-full bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-700">
                      Plan de prueba
                    </span>
                  )}
                </div>
                <p className="text-sm text-slate-500">
                  ${plan.precio.toLocaleString('es-AR')} / {PERIODICIDAD_LABEL[plan.periodicidad]} ·{' '}
                  {plan.limiteRecursos} recurso(s) · {plan.limiteReservasPorMes} reserva(s)/mes
                </p>
              </div>
              <div className="flex flex-wrap gap-2">
                <Button variant="secondary" onClick={() => abrirEdicion(plan)}>
                  Editar
                </Button>
                {!plan.esPlanDePrueba && (
                  <Button
                    variant="secondary"
                    disabled={procesando === plan.id}
                    onClick={() => handleMarcarDePrueba(plan.id)}
                  >
                    Marcar de prueba
                  </Button>
                )}
                {plan.activo && (
                  <Button
                    variant="secondary"
                    disabled={procesando === plan.id}
                    onClick={() => handleDesactivar(plan.id)}
                  >
                    Desactivar
                  </Button>
                )}
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
