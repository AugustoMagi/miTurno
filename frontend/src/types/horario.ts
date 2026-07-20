// DiaSemana espeja System.DayOfWeek de .NET: domingo=0 ... sábado=6 (no es el orden ISO habitual).
export const DIAS_SEMANA: { valor: number; nombre: string }[] = [
  { valor: 0, nombre: 'Domingo' },
  { valor: 1, nombre: 'Lunes' },
  { valor: 2, nombre: 'Martes' },
  { valor: 3, nombre: 'Miércoles' },
  { valor: 4, nombre: 'Jueves' },
  { valor: 5, nombre: 'Viernes' },
  { valor: 6, nombre: 'Sábado' },
]

export interface HorarioDisponible {
  id: string
  recursoId: string
  diaSemana: number
  horaInicio: string // HH:mm:ss
  horaFin: string
}

export interface AgregarHorarioInput {
  diaSemana: number
  horaInicio: string
  horaFin: string
}
