// Fotos reales de distintas canchas, de fondo en el hero: muy transparentes, se van cruzando con
// un fade lento y continuo (puro CSS, ver .animate-cancha-fade en index.css) — ningún deporte
// "gana" sobre otro, es la misma idea de "sirve para cualquier tipo de cancha". En escala de
// grises para que no compitan en color con la marca, siendo sólo una textura de fondo.
const DURACION_S = 30
const CANTIDAD = 5

const FOTOS = [
  { src: '/images/canchas/futbol.jpg', alt: '' },
  { src: '/images/canchas/basquet.jpg', alt: '' },
  { src: '/images/canchas/tenis.jpg', alt: '' },
  { src: '/images/canchas/padel.jpg', alt: '' },
  { src: '/images/canchas/voley.jpg', alt: '' },
]

export function HeroCanchasFondo() {
  return (
    // grid + col-start-1/row-start-1 en cada hijo: los apila a todos en la misma celda. A
    // diferencia de position:absolute sin top/left/right/bottom, esto no depende de la "posición
    // estática" del navegador — es la forma robusta de superponer capas.
    <div className="pointer-events-none absolute inset-0 grid overflow-hidden" aria-hidden="true">
      {FOTOS.map((foto, i) => (
        <img
          key={foto.src}
          src={foto.src}
          alt={foto.alt}
          className="animate-cancha-fade col-start-1 row-start-1 h-full w-full object-cover opacity-0 grayscale"
          style={{ animationDelay: `${-(i * (DURACION_S / CANTIDAD))}s` }}
        />
      ))}
    </div>
  )
}
