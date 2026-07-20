import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getNegocioPublico } from '../api/negociosPublicos'
import { extractError } from '../api/client'
import type { NegocioPublico } from '../types/negocio'
import { Card } from '../components/Card'
import { Spinner } from '../components/Spinner'
import { ErrorBanner } from '../components/ErrorBanner'

export function NegocioPage() {
  const { slug } = useParams<{ slug: string }>()
  const [negocio, setNegocio] = useState<NegocioPublico | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!slug) return
    setLoading(true)
    setError(null)
    getNegocioPublico(slug)
      .then(setNegocio)
      .catch((err) => setError(extractError(err)))
      .finally(() => setLoading(false))
  }, [slug])

  if (loading) return <Spinner label="Buscando el negocio…" />
  if (error) return <ErrorBanner message="Este negocio no existe o no está disponible." />
  if (!negocio) return null

  return (
    <div className="flex flex-col gap-8">
      <div>
        <h1 className="text-2xl font-semibold text-slate-900 sm:text-3xl">{negocio.nombre}</h1>
        {negocio.descripcion && <p className="mt-2 text-slate-600">{negocio.descripcion}</p>}
        {negocio.direccion && <p className="mt-1 text-sm text-slate-400">{negocio.direccion}</p>}
      </div>

      {negocio.recursos.length === 0 ? (
        <p className="text-slate-500">Este negocio todavía no tiene turnos disponibles.</p>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {negocio.recursos.map((recurso) => (
            <Link key={recurso.id} to={`/${slug}/reservar/${recurso.id}`}>
              <Card className="h-full transition-shadow hover:shadow-md">
                <h2 className="font-semibold text-slate-900">{recurso.nombre}</h2>
                <p className="mt-1 text-sm text-slate-500">{recurso.tipo}</p>
                <div className="mt-4 flex items-center justify-between text-sm">
                  <span className="text-slate-500">{recurso.duracionTurnoMinutos} min</span>
                  <span className="font-semibold text-emerald-700">
                    ${recurso.precio.toLocaleString('es-AR')}
                  </span>
                </div>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}
