// ===== Nodo Code: "Construir HTML del dashboard" (versión financiera) =====
const props = $('Query propuestas_proyectos').all().map(i => i.json).filter(p => p && p.id !== undefined);
const gastos = $('Query gastos').all().map(i => i.json).filter(g => g && g.id !== undefined);

// ---------- helpers ----------
function esc(s){ if(s===null||s===undefined) return ''; return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }
function num(v){ if(v===null||v===undefined||v==='') return null; const n=Number(v); return isNaN(n)?null:n; }
function money(n){ if(n===null||n===undefined) return '—'; return '$'+Number(n).toLocaleString('en-US',{minimumFractionDigits:2,maximumFractionDigits:2}); }
function fmtDate(d){ if(!d) return '—'; const s=String(d).slice(0,10); const p=s.split('-'); if(p.length===3) return p[2]+'/'+p[1]+'/'+p[0]; const dt=new Date(d); return isNaN(dt)?String(d):dt.toLocaleDateString('es-VE'); }
function daysUntil(d){ if(!d) return null; const s=String(d).slice(0,10).split('-').map(Number); if(s.length!==3) return null; const now=new Date(); now.setHours(0,0,0,0); const t=new Date(s[0],s[1]-1,s[2]); return Math.round((t-now)/86400000); }
function perMonth(g){ const m=num(g.monto); if(m===null) return null; if(g.periodicidad==='anual') return m/12; return m; } // mensual/consumo => monto

const AMB={
  este:{label:'Automatización (este)',bg:'#e7f4ec',fg:'#14803a'},
  general:{label:'Todos los proyectos',bg:'#e8f0fc',fg:'#1e5fbf'},
  motoapp:{label:'MotoApp',bg:'#efe8fb',fg:'#6b34c9'},
  ehp:{label:'SistemaGestion / EHP',bg:'#fdf0dd',fg:'#a05a00'},
  personal:{label:'Personal',bg:'#eceef1',fg:'#4b5563'}
};
function chip(a){ const x=AMB[a]||{label:a||'—',bg:'#eceef1',fg:'#4b5563'}; return '<span class="chip" style="background:'+x.bg+';color:'+x.fg+'">'+esc(x.label)+'</span>'; }
const perLabel={mensual:'Mensual',anual:'Anual',consumo:'Por consumo'};

// ---------- particionar gastos ----------
const recur = gastos.filter(g=>g.tipo==='recurrente');
const unicos = gastos.filter(g=>g.tipo!=='recurrente');
recur.sort((a,b)=> String(a.proximo_cobro||'9999').localeCompare(String(b.proximo_cobro||'9999')) );
unicos.sort((a,b)=> String(b.created_at||'').localeCompare(String(a.created_at||'')) );

// ---------- KPIs ----------
let totalMo=0; for(const g of recur){ const pm=perMonth(g); if(pm!=null) totalMo+=pm; }
const esteMo = recur.filter(g=>g.ambito==='este').reduce((s,g)=>s+(perMonth(g)||0),0);
const genMo  = recur.filter(g=>g.ambito==='general').reduce((s,g)=>s+(perMonth(g)||0),0);
const totalUni = unicos.reduce((s,g)=>s+(num(g.monto)||0),0);

// ingresos = propuestas ganadas/pagadas (aceptada / en_desarrollo / completado), solo USD
const GANADO=['aceptada','en_desarrollo','completado'];
const ganadas = props.filter(p=> GANADO.indexOf(String(p.estado||'').toLowerCase())>=0 );
const ingUSD = ganadas.filter(p=>String(p.moneda||'').toUpperCase()==='USD').reduce((s,p)=>s+(num(p.monto_estimado)||0),0);
const balance = ingUSD - (totalMo + totalUni);

// resumen por ámbito (mensual)
const ambMo={}; for(const g of recur){ const pm=perMonth(g)||0; ambMo[g.ambito]=(ambMo[g.ambito]||0)+pm; }

// ---------- HTML ----------
const H=[];
H.push('<!DOCTYPE html><html lang="es"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0">');
H.push('<title>Dashboard - Finanzas y Propuestas</title><style>');
H.push('*{box-sizing:border-box;margin:0;padding:0}body{font-family:-apple-system,BlinkMacSystemFont,Roboto,Arial,sans-serif;background:#f4f5f7;color:#1a1a1a;padding:24px 16px 60px}');
H.push('.wrap{max-width:1100px;margin:0 auto}h1{font-size:23px}.sub{color:#666;font-size:13px;margin:3px 0 20px}');
H.push('.kpis{display:grid;grid-template-columns:repeat(auto-fit,minmax(160px,1fr));gap:12px;margin-bottom:22px}');
H.push('.kpi{background:#fff;border:1px solid #e5e8ec;border-radius:12px;padding:13px 15px;box-shadow:0 1px 3px rgba(20,30,50,.07)}');
H.push('.kpi .l{font-size:10.5px;text-transform:uppercase;letter-spacing:.05em;color:#6b7280;font-weight:600}.kpi .v{font-size:23px;font-weight:700;margin-top:5px}.kpi .n{font-size:11px;color:#8a939c;margin-top:2px}');
H.push('.kpi.g .v{color:#14803a}.kpi.r .v{color:#c0392b}');
H.push('.card{background:#fff;border:1px solid #e5e8ec;border-radius:12px;box-shadow:0 1px 3px rgba(20,30,50,.07);margin-bottom:20px;overflow:hidden}');
H.push('.card h2{font-size:15px;padding:13px 16px;border-bottom:1px solid #eef0f2;display:flex;justify-content:space-between;align-items:center}.card h2 .c{font-size:12px;color:#8a939c;font-weight:500}');
H.push('.scroll{overflow-x:auto}table{width:100%;border-collapse:collapse;font-size:13px;min-width:620px}');
H.push('th{text-align:left;font-size:10.5px;text-transform:uppercase;letter-spacing:.04em;color:#6b7280;font-weight:600;padding:9px 12px;border-bottom:1px solid #eef0f2;white-space:nowrap}');
H.push('td{padding:10px 12px;border-bottom:1px solid #f1f3f5;vertical-align:middle}tr:last-child td{border-bottom:none}');
H.push('.svc{font-weight:600}.mut{color:#8a939c;font-size:11.5px}.num{text-align:right;font-variant-numeric:tabular-nums;white-space:nowrap}.amt{font-weight:600}');
H.push('.chip{display:inline-block;font-size:10.5px;font-weight:600;padding:2px 8px;border-radius:20px;white-space:nowrap}');
H.push('.due{display:inline-flex;gap:6px;align-items:center;white-space:nowrap}.badge{font-size:10.5px;font-weight:700;padding:1px 7px;border-radius:20px;background:#eceef1;color:#4b5563}.badge.soon{background:#fbeae7;color:#c0392b}.badge.pend{background:#fdf0dd;color:#a05a00}');
H.push('tfoot td{font-weight:700;background:#f7f8fa;border-top:2px solid #eef0f2}.notec{color:#8a939c;font-size:11.5px;max-width:230px}');
H.push('.amb{display:grid;grid-template-columns:repeat(auto-fit,minmax(185px,1fr));gap:10px;padding:14px 16px}.abox{border:1px solid #eef0f2;border-radius:10px;padding:11px 13px}.abox .t{display:flex;justify-content:space-between;align-items:center;margin-bottom:4px}.abox .m{font-size:18px;font-weight:700}');
H.push('.empty{padding:20px 16px;color:#8a939c;font-size:13px;text-align:center}a{color:#2563eb;text-decoration:none}.foot{color:#8a939c;font-size:11.5px;line-height:1.7;padding:2px 4px}.foot b{color:#333}</style></head><body><div class="wrap">');
H.push('<h1>Dashboard — Finanzas y Propuestas</h1>');
H.push('<div class="sub">Datos en vivo desde PostgreSQL · Generado el '+esc(new Date().toLocaleString('es-VE',{dateStyle:'full',timeStyle:'short'}))+'</div>');

// KPIs
H.push('<div class="kpis">');
H.push('<div class="kpi"><div class="l">Recurrente / mes</div><div class="v">'+money(totalMo)+'</div><div class="n">Todos (+ Hetzner pend.)</div></div>');
H.push('<div class="kpi g"><div class="l">Este proyecto</div><div class="v">'+money(esteMo)+'</div><div class="n">Directo (Upwork+Hetzner)</div></div>');
H.push('<div class="kpi"><div class="l">Compartido</div><div class="v">'+money(genMo)+'</div><div class="n">Anthropic + Google One</div></div>');
H.push('<div class="kpi"><div class="l">Gastos únicos</div><div class="v">'+money(totalUni)+'</div><div class="n">Connects + créditos</div></div>');
H.push('<div class="kpi"><div class="l">Ingresos ganados</div><div class="v">'+money(ingUSD)+'</div><div class="n">'+ganadas.length+' trabajos ganados</div></div>');
H.push('<div class="kpi '+(balance<0?'r':'g')+'"><div class="l">Balance del mes</div><div class="v">'+money(balance)+'</div><div class="n">Ingresos − gastos</div></div>');
H.push('</div>');

// Recurrentes
H.push('<div class="card"><h2>Gastos recurrentes <span class="c">'+recur.length+' servicios</span></h2><div class="scroll"><table><thead><tr><th>Servicio</th><th>Proyecto</th><th>Periodicidad</th><th class="num">Monto</th><th class="num">Equiv./mes</th><th>Próximo cobro</th><th>Nota</th></tr></thead><tbody>');
for(const g of recur){
  const pm=perMonth(g); const du=daysUntil(g.proximo_cobro); const pend=(num(g.monto)===null);
  let bc='badge',bt='';
  if(pend){bc+=' pend';bt='pendiente';}
  else if(du!==null){bt=(du<=0?'hoy':'en '+du+'d'); if(du<=7)bc+=' soon';}
  H.push('<tr><td><div class="svc">'+esc(g.descripcion)+'</div><div class="mut">'+esc(g.categoria)+'</div></td><td>'+chip(g.ambito)+'</td><td class="mut">'+(perLabel[g.periodicidad]||'')+'</td><td class="num amt">'+money(num(g.monto))+(g.periodicidad==='consumo'?' <span class="mut">≈</span>':'')+'</td><td class="num">'+(pm===null?'—':money(pm))+'</td><td><span class="due">'+fmtDate(g.proximo_cobro)+' <span class="'+bc+'">'+bt+'</span></span></td><td class="notec">'+esc(g.notas)+'</td></tr>');
}
H.push('</tbody><tfoot><tr><td colspan="4">Total equivalente mensual</td><td class="num">'+money(totalMo)+'</td><td colspan="2" class="mut">+ Hetzner por confirmar</td></tr></tfoot></table></div></div>');

// Únicos
H.push('<div class="card"><h2>Gastos únicos / extras <span class="c">'+unicos.length+' movimientos</span></h2><div class="scroll"><table><thead><tr><th>Concepto</th><th>Proyecto</th><th>Fecha</th><th class="num">Monto</th><th>Nota</th></tr></thead><tbody>');
for(const g of unicos){
  H.push('<tr><td><div class="svc">'+esc(g.descripcion)+'</div><div class="mut">'+esc(g.categoria)+'</div></td><td>'+chip(g.ambito)+'</td><td class="mut">'+fmtDate(g.created_at)+'</td><td class="num amt">'+money(num(g.monto))+'</td><td class="notec">'+esc(g.notas)+'</td></tr>');
}
H.push('</tbody><tfoot><tr><td colspan="3">Total gastos únicos</td><td class="num">'+money(totalUni)+'</td><td></td></tr></tfoot></table></div></div>');

// Resumen por ámbito
H.push('<div class="card"><h2>Gasto mensual por proyecto</h2><div class="amb">');
for(const k of Object.keys(AMB)){ if(ambMo[k]===undefined) continue; H.push('<div class="abox"><div class="t">'+chip(k)+'<span class="m">'+money(ambMo[k])+'</span></div><div class="mut">'+esc(recur.filter(g=>g.ambito===k).map(g=>String(g.descripcion).split(' — ')[0].split(' (')[0]).join(' · '))+(k==='este'?' · +Hetzner pend.':'')+'</div></div>'); }
H.push('</div></div>');

// Propuestas (se conserva)
let estadoCounts={}; for(const p of props){ const e=p.estado||'sin estado'; estadoCounts[e]=(estadoCounts[e]||0)+1; }
let estParts=[]; for(const k in estadoCounts){ estParts.push(esc(k)+': '+estadoCounts[k]); }
H.push('<div class="card"><h2>Propuestas y proyectos <span class="c">'+props.length+'</span></h2><div class="scroll">');
H.push('<div style="padding:10px 14px;color:#444;font-size:13px;background:#f7f8fa;border-radius:6px;margin:12px 16px">'+(estParts.join(' · ')||'Sin propuestas')+'</div>');
if(props.length){
  H.push('<table><thead><tr><th>Fecha</th><th>Plataforma</th><th>Cliente</th><th>Proyecto</th><th>Estado</th><th class="num">Monto</th><th>URL</th><th>Notas</th></tr></thead><tbody>');
  for(const p of props){
    const mo=(num(p.monto_estimado)!==null)?(Number(p.monto_estimado).toLocaleString('en-US',{minimumFractionDigits:2,maximumFractionDigits:2})+(p.moneda?' '+esc(p.moneda):'')):'';
    H.push('<tr><td class="mut">'+fmtDate(p.fecha_actualizacion||p.fecha_envio)+'</td><td>'+esc(p.plataforma)+'</td><td>'+esc(p.cliente)+'</td><td class="svc">'+esc(p.proyecto)+'</td><td>'+esc(p.estado)+'</td><td class="num">'+mo+'</td><td>'+(p.url?'<a href="'+esc(p.url)+'" target="_blank">link</a>':'')+'</td><td class="notec">'+esc(p.notas)+'</td></tr>');
  }
  H.push('</tbody></table>');
} else { H.push('<div class="empty">No hay propuestas registradas todavía.</div>'); }
H.push('</div></div>');

H.push('<div class="foot"><b>Notas:</b> montos en USD con impuestos donde aplica. <b>Hetzner</b>: sin primera factura aún (factura el 28), monto por confirmar. <b>Vercel</b>: por consumo (BD Neon de MotoApp), varía. <b>Cloudflare</b>: dominio de SistemaGestion/EHP (otro proyecto). <b>Ingresos</b>: solo trabajos ganados/pagados (estado aceptada/en_desarrollo/completado, USD); una propuesta enviada no es ingreso. Recarga la página para datos frescos.</div>');
H.push('</div></body></html>');

return [{ json: { html: H.join('') } }];
