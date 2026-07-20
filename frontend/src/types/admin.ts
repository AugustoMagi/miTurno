export interface SesionAdmin {
  sysAdminId: string
  nombre: string
  email: string
  token: string
}

export interface LoginAdminInput {
  email: string
  password: string
}
