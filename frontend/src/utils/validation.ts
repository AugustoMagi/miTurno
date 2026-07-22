// Validaciones de formularios del lado del cliente.
// Cada función devuelve un mensaje de error en español o null/undefined si el valor es válido,
// para poder mostrarlo directamente debajo del input correspondiente.

const REGEX_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const REGEX_SLUG = /^[a-z0-9]+(-[a-z0-9]+)*$/
const REGEX_TELEFONO = /^[0-9+\-\s()]+$/

export function esSoloEspacios(valor: string): boolean {
  return valor.length > 0 && valor.trim().length === 0
}

export function validarRequerido(valor: string, etiqueta: string): string | undefined {
  if (!valor || valor.trim().length === 0) return `${etiqueta} es obligatorio.`
  if (esSoloEspacios(valor)) return `${etiqueta} no puede contener solo espacios.`
  return undefined
}

export function validarEmail(valor: string, etiqueta = 'El email'): string | undefined {
  const requerido = validarRequerido(valor, etiqueta)
  if (requerido) return requerido
  if (!REGEX_EMAIL.test(valor.trim())) {
    return 'Ingresá un email válido (ej: nombre@dominio.com).'
  }
  return undefined
}

export function validarTelefono(valor: string): string | undefined {
  const limpio = valor.trim()
  if (!limpio) return undefined // es opcional
  if (!REGEX_TELEFONO.test(limpio)) {
    return 'El teléfono solo puede contener números, espacios, +, - o paréntesis.'
  }
  const soloDigitos = limpio.replace(/\D/g, '')
  if (soloDigitos.length < 6) {
    return 'El teléfono debe tener al menos 6 números.'
  }
  if (soloDigitos.length > 20) {
    return 'El teléfono ingresado es demasiado largo.'
  }
  return undefined
}

export function validarPassword(valor: string, minimo = 8): string | undefined {
  if (!valor) return 'La contraseña es obligatoria.'
  if (valor.length < minimo) return `La contraseña debe tener al menos ${minimo} caracteres.`
  if (esSoloEspacios(valor)) return 'La contraseña no puede contener solo espacios.'
  return undefined
}

export function validarConfirmacionPassword(
  password: string,
  confirmacion: string,
): string | undefined {
  if (!confirmacion) return 'Repetí la contraseña.'
  if (password !== confirmacion) return 'Las contraseñas no coinciden.'
  return undefined
}

export function validarSlug(valor: string): string | undefined {
  const requerido = validarRequerido(valor, 'La URL')
  if (requerido) return requerido
  if (valor.length < 3) return 'La URL debe tener al menos 3 caracteres.'
  if (!REGEX_SLUG.test(valor)) {
    return 'La URL solo puede tener minúsculas, números y guiones, sin empezar, terminar ni tener guiones repetidos.'
  }
  return undefined
}

export function validarNumeroNoNegativo(valor: number, etiqueta: string): string | undefined {
  if (Number.isNaN(valor)) return `${etiqueta} debe ser un número.`
  if (valor < 0) return `${etiqueta} no puede ser negativo.`
  return undefined
}

export function validarEntero(
  valor: number,
  etiqueta: string,
  minimo = 1,
): string | undefined {
  if (Number.isNaN(valor)) return `${etiqueta} debe ser un número.`
  if (!Number.isInteger(valor)) return `${etiqueta} debe ser un número entero.`
  if (valor < minimo) return `${etiqueta} debe ser mayor o igual a ${minimo}.`
  return undefined
}

export function validarRangoHorario(horaInicio: string, horaFin: string): string | undefined {
  if (horaInicio && horaFin && horaInicio >= horaFin) {
    return 'El horario "desde" debe ser anterior al horario "hasta".'
  }
  return undefined
}

export function validarRangoFechas(desde: string, hasta: string): string | undefined {
  if (desde && hasta && desde > hasta) {
    return 'La fecha "desde" no puede ser posterior a la fecha "hasta".'
  }
  return undefined
}

export function validarFechaNoPasada(fecha: string, hoyIso: string): string | undefined {
  if (fecha && fecha < hoyIso) {
    return 'La fecha no puede ser anterior a hoy.'
  }
  return undefined
}

export function validarAlias(valor: string): string | undefined {
  const requerido = validarRequerido(valor, 'El alias o CVU')
  if (requerido) return requerido
  if (valor.trim().length < 6) return 'El alias o CVU debe tener al menos 6 caracteres.'
  return undefined
}
