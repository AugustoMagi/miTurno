export interface Sesion {
  usuarioId: string
  negocioId: string
  nombre: string
  email: string
  rol: string
  token: string
}

export interface LoginInput {
  email: string
  password: string
}

export interface RegistroInput {
  nombreNegocio: string
  slug: string
  emailNegocio: string
  nombreUsuario: string
  emailUsuario: string
  password: string
}
