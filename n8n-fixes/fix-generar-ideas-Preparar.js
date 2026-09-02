// ===== Nodo Code "Preparar" — workflow "Contenido — Generar Ideas (IA)" (CORREGIDO) =====
// Arregla el error de Gemini "parts[0].data ... must have one initialized field":
// antes el nodo no creaba el campo `prompt` que usa el nodo "Gemini Ideas".
const b = $input.first().json.body || $input.first().json;
const marca = b.marca || {};
const conIngles = String(marca.idiomas || 'es').indexOf('en') >= 0;
const cantidad = Number(b.cantidad || b.cantidadIdeas || 10);
const pilares = marca.pilares || b.pilares || '';

const prompt =
  `Eres estratega de contenido y community manager de la marca "${marca.nombre || ''}".\n` +
  `Voz/tono: ${marca.voz || ''}. Guia de estilo: ${marca.guiaEstilo || ''}.\n` +
  (pilares ? `Pilares de contenido: ${pilares}.\n` : '') +
  `Genera ${cantidad} ideas de publicaciones para Instagram/Facebook, variadas y fieles a la marca.\n` +
  `Cada idea debe tener: un "titulo" corto, una "descripcion" de 1-2 frases sobre de que trata el post, ` +
  `y el "pilar" de contenido al que pertenece.\n` +
  (conIngles ? `La marca maneja espanol e ingles; los campos van en espanol.\n` : ``) +
  `Responde UNICAMENTE con un arreglo JSON valido, sin texto adicional ni bloques de codigo, con esta forma exacta:\n` +
  `[{"titulo":"...","descripcion":"...","pilar":"..."}]`;

const marcaId = b.marcaId || marca.id || marca.marcaId || null;

return [{ json: {
  prompt,
  marcaId,
  callbackUrl: b.callbackUrl,
  jobId: b.jobId,
  postId: b.postId
} }];
