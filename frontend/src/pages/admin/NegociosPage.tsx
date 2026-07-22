import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { activarNegocio, desactivarNegocio, listarNegocios } from '../../api/negociosAdmin'
import { extractError } from '../../api/client'
import type { NegocioAdmin } from '../../types/negocioAdmin'
import { Card } from '../../components/Card'
import { Spinner } from '../../components/Spinner'
import { ErrorBanner } from '../../components/ErrorBanner'

export function NegociosPage() {
  const [negocios, setNegocios] = useState<NegocioAdmin[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [procesando, setProcesando] = useState<string | null>(null)

  function cargar() {
    setError(null)
    listarNegocios()
      .then(setNegocios)
      .catch((err) => setError(extractError(err)))
  }

  useEffect(cargar, [])

  async function handleCambiarEstado(negocio: NegocioAdmin) {
    setProcesando(negocio.id)
    setError(null)
    try {
      if (negocio.activo) await desactivarNegocio(negocio.id)
      else await activarNegocio(negocio.id)
      cargar()
    } catch (err) {
      setError(extractError(err))
    } finally {
      setProcesando(null)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold text-slate-900">Negocios</h1>

      {error && <ErrorBanner message={error} />}

      {!negocios ? (
        <Spinner />
      ) : negocios.length === 0 ? (
        <p className="text-slate-500">Todavía no hay negocios registrados.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {negocios.map((negocio) => (
            <Card key={negocio.id} className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <Link
                    to={`/admin/negocios/${negocio.id}`}
                    className="font-semibold text-slate-900 hover:underline"
                  >
                    {negocio.nombre}
                  </Link>
                  <span
                    className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                      negocio.activo ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'
                    }`}
                  >
                    {negocio.activo ? 'Activo' : 'Inactivo'}
                  </span>
                </div>
                <p className="text-sm text-slate-500">
                  {negocio.email} · miturno.app/{negocio.slug}
                </p>
              </div>
              <div className="flex gap-2">
                <Link to={`/admin/negocios/${negocio.id}`}>
                  <button
                    type="button"
                    className="rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-100"
                  >
                    Ver detalle
                  </button>
                </Link>
                <button
                  type="button"
                  disabled={procesando === negocio.id}
                  onClick={() => handleCambiarEstado(negocio)}
                  className="rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {negocio.activo ? 'Desactivar' : 'Activar'}
                </button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
