import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function RequireAuth() {
  const { sesion } = useAuth()

  if (!sesion) {
    return <Navigate to="/panel/login" replace />
  }

  return <Outlet />
}
