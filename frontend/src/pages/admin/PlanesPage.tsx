import { useEffect, useState } from 'react'
import {
  actualizarPlan,
  crearPlan,
  desactivarPlan,
  desmarcarPlanDePrueba,
  eliminarPlan,
  listarPlanes,
  marcarPlanDePrueba,
} from '../../api/planes'
import { extractError } from '../../api/client'
import { Periodicidad } from '../../types/plan'
import type { Plan, PlanInput } from '../../types/plan'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'
import { Field, Input, Select } from '../../components/Input'
import { PlusIcon, TrashIcon, XIcon } from '../../components/icons'
import { validarEntero, validarNumeroNoNegativo, validarRequerido } from '../../utils/validation'

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
  const [tocado, setTocado] = useState<Record<string, boolean>>({})

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
    setTocado({})
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
    setTocado({})
    setMostrarForm(true)
  }

  const errorNombre = validarRequerido(form.nombre, 'El nombre')
  const errorPrecio = validarNumeroNoNegativo(form.precio, 'El precio')
  const errorLimiteRecursos = validarEntero(form.limiteRecursos, 'El límite de recursos', 1)
  const errorLimiteReservas = validarEntero(form.limiteReservasPorMes, 'El límite de reservas por mes', 1)

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setTocado({ nombre: true, precio: true, limiteRecursos: true, limiteReservasPorMes: true })
    if (errorNombre || errorPrecio || errorLimiteRecursos || errorLimiteReservas) return
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

  async function handleDesmarcarDePrueba(id: string) {
    setProcesando(id)
    setError(null)
    try {
      await desmarcarPlanDePrueba(id)
      cargar()
    } catch (err) {
      setError(extractError(err))
    } finally {
      setProcesando(null)
    }
  }

  async function handleEliminar(id: string, nombre: string) {
    if (!window.confirm(`¿Eliminar el plan "${nombre}"? Esta acción no se puede deshacer.`)) return
    setProcesando(id)
    setError(null)
    try {
      await eliminarPlan(id)
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
        <Button
          variant={mostrarForm && !editandoId ? 'secondary' : 'primary'}
          icon={mostrarForm && !editandoId ? <XIcon /> : <PlusIcon />}
          onClick={mostrarForm && !editandoId ? () => setMostrarForm(false) : abrirNuevo}
        >
          {mostrarForm && !editandoId ? 'Cancelar' : 'Nuevo plan'}
        </Button>
      </div>

      {mostrarForm && (
        <Card className="animate-scale-in">
          <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
            <h2 className="font-semibold text-slate-900">{editandoId ? 'Editar plan' : 'Nuevo plan'}</h2>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field label="Nombre" error={tocado.nombre ? errorNombre : undefined} required>
                <Input
                  type="text"
                  required
                  maxLength={100}
                  value={form.nombre}
                  onChange={(event) => setForm({ ...form, nombre: event.target.value })}
                  onBlur={() => setTocado((t) => ({ ...t, nombre: true }))}
                  aria-invalid={Boolean(tocado.nombre && errorNombre)}
                />
              </Field>
              <Field label="Precio" error={tocado.precio ? errorPrecio : undefined} required>
                <Input
                  type="number"
                  required
                  min={0}
                  step="0.01"
                  value={form.precio}
                  onChange={(event) => setForm({ ...form, precio: Number(event.target.value) })}
                  onBlur={() => setTocado((t) => ({ ...t, precio: true }))}
                  aria-invalid={Boolean(tocado.precio && errorPrecio)}
                />
              </Field>
              <Field label="Periodicidad">
                <Select
                  value={form.periodicidad}
                  onChange={(event) => setForm({ ...form, periodicidad: Number(event.target.value) as Periodicidad })}
                >
                  <option value={Periodicidad.Mensual}>Mensual</option>
                  <option value={Periodicidad.Anual}>Anual</option>
                </Select>
              </Field>
              <div />
              <Field label="Límite de recursos" error={tocado.limiteRecursos ? errorLimiteRecursos : undefined} required>
                <Input
                  type="number"
                  required
                  min={1}
                  value={form.limiteRecursos}
                  onChange={(event) => setForm({ ...form, limiteRecursos: Number(event.target.value) })}
                  onBlur={() => setTocado((t) => ({ ...t, limiteRecursos: true }))}
                  aria-invalid={Boolean(tocado.limiteRecursos && errorLimiteRecursos)}
                />
              </Field>
              <Field
                label="Límite de reservas por mes"
                error={tocado.limiteReservasPorMes ? errorLimiteReservas : undefined}
                required
              >
                <Input
                  type="number"
                  required
                  min={1}
                  value={form.limiteReservasPorMes}
                  onChange={(event) => setForm({ ...form, limiteReservasPorMes: Number(event.target.value) })}
                  onBlur={() => setTocado((t) => ({ ...t, limiteReservasPorMes: true }))}
                  aria-invalid={Boolean(tocado.limiteReservasPorMes && errorLimiteReservas)}
                />
              </Field>
            </div>
            {formError && <ErrorBanner message={formError} />}
            <Button type="submit" loading={guardando} className="self-start">
              {editandoId ? 'Guardar cambios' : 'Crear plan'}
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
            <Card key={plan.id} hover className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
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
                    <span className="rounded-full bg-accent-50 px-2 py-0.5 text-xs font-medium text-accent-700">
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
                <Button variant="secondary" size="sm" onClick={() => abrirEdicion(plan)}>
                  Editar
                </Button>
                {plan.esPlanDePrueba ? (
                  <Button
                    variant="secondary"
                    size="sm"
                    loading={procesando === plan.id}
                    onClick={() => handleDesmarcarDePrueba(plan.id)}
                  >
                    Desmarcar de prueba
                  </Button>
                ) : (
                  <Button
                    variant="secondary"
                    size="sm"
                    loading={procesando === plan.id}
                    onClick={() => handleMarcarDePrueba(plan.id)}
                  >
                    Marcar de prueba
                  </Button>
                )}
                {plan.activo && (
                  <Button
                    variant="secondary"
                    size="sm"
                    loading={procesando === plan.id}
                    onClick={() => handleDesactivar(plan.id)}
                  >
                    Desactivar
                  </Button>
                )}
                <Button
                  variant="secondary"
                  size="sm"
                  icon={<TrashIcon />}
                  loading={procesando === plan.id}
                  onClick={() => handleEliminar(plan.id, plan.nombre)}
                  className="border-red-300 text-red-600 hover:bg-red-50"
                >
                  Eliminar
                </Button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
