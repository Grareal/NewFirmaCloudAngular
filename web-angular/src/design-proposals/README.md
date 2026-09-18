# Propuestas visuales Grupo Vidanta

La aplicación usa por defecto la **Propuesta 01 — Bienvenida Vidanta**, compuesta por
`src/styles.css` y `src/vidanta-design.css`.

Alternativas:

1. **Bienvenida Vidanta**: equilibrio entre hospitalidad y operación; es la activa.
2. **Brisa Caribe**: clara, suave y enfocada en atención al huésped.
3. **Azul Ejecutivo**: compacta, sobria y enfocada en áreas operativas.
4. **Concierge Digital**: cambia el shell y convierte las ventanas en paneles flotantes.

## Activar una alternativa

En `angular.json`, dentro de `projects.web-angular.architect.build.options.styles`,
reemplace `src/styles.css` por una de estas entradas:

- `src/design-proposals/styles-propuesta-02.css`
- `src/design-proposals/styles-propuesta-03.css`
- `src/design-proposals/styles-propuesta-04.css`

Cada entrada carga la base común y después aplica únicamente las decisiones visuales
de su propuesta. Todas conservan `#003594`, `#0095c8`, `#4d4d4d`, Verdana y Arial Narrow.
