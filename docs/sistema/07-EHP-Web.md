# 07 — Web pública EHP Saddles (www.ehp-saddles.com)

La web pública de EHP Saddles es un **producto aparte** del ERP: un sitio de marketing/catálogo, con su **propio repositorio** (`github.com/od1n/ehp-web`) y su propio despliegue (hosting web, no OLIMPO). Se documenta aquí porque forma parte del ecosistema de la empresa y comparte marca con el módulo de redes.

> No es parte del ERP ni de su base de datos. No se mezcla con `Prueba d cld`. Se mantiene como repositorio independiente.

## 1. Qué es
Sitio **estático** (HTML + CSS + JS, sin framework ni build) para El Hato Pascher / EHP Saddles — fabricantes de **fustes en HDPE virgen** (no monturas completas), desde 1983, Villa de Cura, Venezuela. Enfocado en catálogo, historia, servicio B2B de moldes a medida, blog y SEO.

## 2. Estructura
```
index.html            Home: hero, catálogo muestra, historia, sostenibilidad, cómo comprar, FAQ, contacto
productos.html        Catálogo completo (18 modelos) con filtros
producto.html         Ficha dinámica (?id=slug)
nosotros.html         Historia, taller, por qué HDPE, mapa
moldes-a-medida.html  Servicio B2B (moldes propios)
blog.html + blog/     Índice y posts (HDPE vs madera, biomecánica, cómo medir, historia)
glosario.html         24 términos del oficio + diagrama SVG
404.html · sitemap.xml · robots.txt · .htaccess
assets/  ehp.css (design system), post.css, products.js (datos de 18 productos: EHP_PRODUCTS)
docs/    google-business-profile.md
2023/    respaldo del sitio WordPress/WooCommerce anterior (histórico, no se sirve)
```

## 3. Sistema visual
- **Paleta:** cuero quemado, tabaco, crema, verde campo (variables CSS en `assets/ehp.css`).
- **Tipografía:** Fraunces (display), Archivo (UI), IBM Plex Mono (etiquetas), vía Google Fonts.
- **Layout:** editorial, retícula amplia, énfasis tipográfico.

## 4. SEO y analítica
- Meta description + Open Graph + Twitter Cards por página; `canonical` + `hreflang` (ES/EN).
- JSON-LD: Organization, LocalBusiness, FAQPage, Product, ItemList, BreadcrumbList, Service, Blog.
- `sitemap.xml` (todas las páginas + fichas de producto), `robots.txt` abierto.
- **Microsoft Clarity** en las 12 páginas (ID `wcfp34un3j`, panel clarity.microsoft.com).

## 5. Datos de la empresa (para la web)
- Fundación 1983 · Villa de Cura, Aragua (ZIP 2026) · 10°02'13"N 67°29'08"W.
- Tel/WhatsApp +58 244-3861013 · info@ehp-saddle.com · @elhatopascher (IG/FB).
- Exporta a Colombia, México, EE.UU., Costa Rica, Europa, Australia.
- Producto principal: **fustes en HDPE virgen**. Servicio B2B: moldes a medida.

## 6. Pendientes para producción (de su ESTADO-PROYECTO)
1. Subir **fotos propias** (hoy las imágenes vienen del CDN del sitio antiguo) y actualizar `assets/products.js` + hero.
2. Crear el **Google Business Profile** (guía en `docs/google-business-profile.md`).
3. Conectar el **formulario de newsletter** (hoy decorativo) a Mailchimp/Brevo/MailerLite.
4. Crear imágenes **OG** (1200×630) por página.
5. Verificar **URLs canonical** según cómo sirva el hosting (`/productos` vs `.html`).

## 7. Relación con el resto del sistema
- **Marca compartida** con el módulo de redes (Estudio de Contenido) del ERP: mismos productos, tono e identidad. El contenido generado en el ERP puede referir a esta web.
- **Independiente en código y deploy**: repo `ehp-web`, hosting web propio. No toca la BD del ERP ni corre en OLIMPO/SERVO.
- Existe además una carpeta `ehp-bot/` **dentro del repo del ERP** (`Prueba d cld/ehp-bot/`): revisar su propósito (posible bot/automatización asociada a EHP) y decidir si se mantiene ahí o se mueve al repo de la web.
