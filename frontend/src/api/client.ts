import axios from 'axios'
import { clearSesion, getSesion } from '../auth/session'
import { clearSesionAdmin, getSesionAdmin } from '../auth/sessionAdmin'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'https://localhost:7242',
})

function esRutaAdmin(url?: string): boolean {
  return (url ?? '').startsWith('/api/admin')
}

// El dueño y el SysAdmin pueden tener sesión al mismo tiempo en el mismo navegador (son
// identidades distintas); el token a usar depende de a qué parte de la API le pega el request.
apiClient.interceptors.request.use((config) => {
  const sesion = esRutaAdmin(config.url) ? getSesionAdmin() : getSesion()
  if (sesion) {
    config.headers.Authorization = `Bearer ${sesion.token}`
  }
  return config
})

// Si el token expiró o es inválido, cerramos la sesión correspondiente y volvemos al login
// correspondiente, en vez de dejar que cada página maneje el 401 por su cuenta.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      if (esRutaAdmin(error.config?.url)) {
        clearSesionAdmin()
        if (!window.location.pathname.startsWith('/admin/login')) {
          window.location.href = '/admin/login'
        }
      } else {
        clearSesion()
        if (!window.location.pathname.startsWith('/panel/login')) {
          window.location.href = '/panel/login'
        }
      }
    }
    return Promise.reject(error)
  },
)

// Todos los endpoints de MiTurno devuelven errores como { error: "mensaje" } (patrón Result
// consistente en todos los controllers) — este helper extrae ese mensaje o cae a uno genérico.
export function extractError(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const apiError = error.response?.data?.error
    if (typeof apiError === 'string') return apiError
    if (error.message) return error.message
  }
  return 'Ocurrió un error inesperado. Probá de nuevo.'
}
