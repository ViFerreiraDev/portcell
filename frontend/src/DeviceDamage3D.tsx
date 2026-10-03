import { useRef, useState, type CSSProperties, type MouseEvent, type PointerEvent, type ReactNode } from 'react';
import { Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle, Stack, TextField, Typography } from '@mui/material';
import { damageLabel, deviceComponents, deviceFaces, pointDefects, type DamagePoint, type DeviceComponent, type DeviceFace } from './device-components';
import { faceOrientations, visibleFaces, type Orientation } from './device-orientation';
import './device-damage.css';

export type { DamagePoint } from './device-components';
type Editing = { point: DamagePoint; index: number; component?: DeviceComponent };
const generalDefects = ['Riscado', 'Amassado', 'Trincado / quebrado', 'Ausente', 'Descolado', 'Oxidação aparente'];

export function DeviceDamage3D({ points, onChange }: { points: DamagePoint[]; onChange: (points: DamagePoint[]) => void }) {
  const [orientation, setOrientation] = useState<Orientation>(faceOrientations.frente);
  const [dragging, setDragging] = useState(false); const [freeMark, setFreeMark] = useState(false);
  const gesture = useRef<{ id: number; x: number; y: number; origin: Orientation; moved: boolean } | null>(null);
  const suppressClick = useRef(false);
  const visibility = visibleFaces(orientation);
  const face = deviceFaces.reduce((best, item) => visibility[item.key] > visibility[best] ? item.key : best, 'frente' as DeviceFace);
  const isVisible = (key: DeviceFace) => visibility[key] > .12;
  const [hovered, setHovered] = useState('');
  const [editing, setEditing] = useState<Editing | null>(null);
  const [defects, setDefects] = useState<string[]>([]); const [customDefect, setCustomDefect] = useState('');
  const [observation, setObservation] = useState(''); const [dialogError, setDialogError] = useState('');
  const selected = (id: string) => points.some(point => point.componente === id);
  const currentComponents = deviceComponents.filter(component => isVisible(component.face) || component.id === 'bateria');

  function setFace(next: DeviceFace) { setOrientation(faceOrientations[next]); setHovered(''); }
  function startDrag(event: PointerEvent<HTMLDivElement>) {
    if (!event.isPrimary || event.button !== 0) return;
    suppressClick.current = false;
    gesture.current = { id: event.pointerId, x: event.clientX, y: event.clientY, origin: orientation, moved: false };
  }
  function moveDrag(event: PointerEvent<HTMLDivElement>) {
    const current = gesture.current;
    if (!current || current.id !== event.pointerId) return;
    if (!(event.buttons & 1)) { endDrag(event); return; }
    const dx = event.clientX - current.x; const dy = event.clientY - current.y;
    if (!current.moved && Math.hypot(dx, dy) < 6) return;
    current.moved = true; suppressClick.current = true;
    event.currentTarget.setPointerCapture(event.pointerId);
    setDragging(true); setHovered('');
    setOrientation({ x: Math.max(-80, Math.min(80, current.origin.x - dy * .45)), y: current.origin.y + dx * .55 });
  }
  function endDrag(event: PointerEvent<HTMLDivElement>) {
    if (gesture.current?.id !== event.pointerId) return;
    gesture.current = null; setDragging(false);
    if (event.currentTarget.hasPointerCapture(event.pointerId)) event.currentTarget.releasePointerCapture(event.pointerId);
  }
  function openEditor(point: DamagePoint, index: number, component?: DeviceComponent) {
    setEditing({ point, index, component }); setDefects(pointDefects(point));
    setObservation(point.observacao ?? ''); setCustomDefect(''); setDialogError('');
  }
  function inspect(component: DeviceComponent) {
    const index = points.findIndex(point => point.componente === component.id);
    openEditor(index >= 0 ? points[index] : { face: component.face, x: component.x, y: component.y, componente: component.id }, index, component);
  }
  function markLocation(side: DeviceFace, event?: MouseEvent<HTMLButtonElement>) {
    // offsetX/Y are local CSS coordinates, including on a transformed face.
    const x = !event || event.detail === 0 ? .5 : event.nativeEvent.offsetX / event.currentTarget.clientWidth;
    const y = !event || event.detail === 0 ? .5 : event.nativeEvent.offsetY / event.currentTarget.clientHeight;
    const point: DamagePoint = { face: side, x: Math.round(Math.max(0, Math.min(1, x)) * 10000) / 10000, y: Math.round(Math.max(0, Math.min(1, y)) * 10000) / 10000 };
    const index = points.findIndex(item => !item.componente && item.face === side && Math.abs(item.x - point.x) < .015 && Math.abs(item.y - point.y) < .015);
    openEditor(index >= 0 ? points[index] : point, index);
  }
  function toggleDefect(value: string) {
    if (!defects.includes(value) && defects.length >= 8) { setDialogError('Selecione até 8 problemas neste local.'); return; }
    setDefects(defects.includes(value) ? defects.filter(item => item !== value) : [...defects, value]); setDialogError('');
  }
  function addCustom() {
    const value = customDefect.trim();
    if (!value) return;
    if (defects.some(item => item.toLocaleLowerCase('pt-BR') === value.toLocaleLowerCase('pt-BR'))) { setCustomDefect(''); return; }
    if (defects.length >= 8) { setDialogError('Selecione até 8 problemas neste local.'); return; }
    setDefects([...defects, value]); setCustomDefect(''); setDialogError('');
  }
  function saveDefects() {
    if (!editing) return;
    const extra = customDefect.trim();
    const findings = extra && !defects.some(item => item.toLocaleLowerCase('pt-BR') === extra.toLocaleLowerCase('pt-BR')) ? [...defects, extra] : defects;
    if (!findings.length && !observation.trim()) { setDialogError('Selecione um problema ou descreva o que foi observado neste local.'); return; }
    if (findings.length > 8) { setDialogError('O limite é de 8 problemas por local.'); return; }
    if (editing.index < 0 && points.length >= 24) { setDialogError('O limite é de 24 locais de avaria. Edite um registro existente.'); return; }
    const saved: DamagePoint = { ...editing.point, defeito: undefined, defeitos: findings, observacao: observation.trim() || undefined };
    onChange(editing.index < 0 ? [...points, saved] : points.map((point, index) => index === editing.index ? saved : point));
    setEditing(null);
  }
  function hotspot(id: string, className: string, children?: ReactNode) {
    const component = deviceComponents.find(item => item.id === id)!;
    return <button type="button" className={`phone-part ${className}`} data-defect={selected(id)} aria-label={`Inspecionar ${component.label.toLocaleLowerCase('pt-BR')}`} aria-pressed={selected(id)} aria-haspopup="dialog"
      tabIndex={isVisible(component.face) && !freeMark ? 0 : -1} onMouseEnter={() => setHovered(component.label)} onMouseLeave={() => setHovered('')} onFocus={() => setHovered(component.label)} onBlur={() => setHovered('')} onClick={() => inspect(component)}>{children}</button>;
  }
  function surface(side: DeviceFace) {
    return <button type="button" className="phone-general-surface" aria-label={`Marcar local na face ${deviceFaces.find(item => item.key === side)?.label.toLocaleLowerCase('pt-BR')}`} tabIndex={isVisible(side) ? 0 : -1} aria-haspopup="dialog" onClick={event => markLocation(side, event)}/>;
  }
  function markers(side: DeviceFace) {
    return points.map((point, index) => point.face === side && !point.componente && <button key={index} type="button" className="phone-marker" style={{ left: `${point.x * 100}%`, top: `${point.y * 100}%` }} tabIndex={isVisible(side) ? 0 : -1} aria-label={`Editar marcação ${index + 1} na face ${side}`} onClick={() => openEditor(point, index)}>{index + 1}</button>);
  }
  const options = editing?.component?.defects ?? generalDefects;

  return <Box className="device-inspection">
    <div className="inspection-heading"><div><Typography variant="h6">Inspeção do aparelho</Typography><Typography color="text.secondary" sx={{ fontSize: 13, mt: .5 }}>Arraste para girar. Clique em um componente ou marque um local livre.</Typography></div><span className={`inspection-count ${points.length ? 'has-findings' : ''}`}>{points.length} {points.length === 1 ? 'local registrado' : 'locais registrados'}</span></div>
    <div className="inspection-workspace"><div className="phone-viewer">
      <div className="phone-mark-mode" aria-label="Modo de inspeção"><button type="button" aria-pressed={!freeMark} onClick={() => setFreeMark(false)}>Componentes</button><button type="button" aria-pressed={freeMark} onClick={() => setFreeMark(true)}>Marcar local livre</button></div>
      <p className="phone-mode-hint">{freeMark ? 'Clique no ponto exato de qualquer face. Riscos, amassados e outras avarias.' : 'Selecione uma peça ou toque na carcaça para registrar uma avaria.'}</p>
      <div className={`phone-stage ${dragging ? 'is-dragging' : ''} ${freeMark ? 'phone-free-mode' : ''}`} onPointerDown={startDrag} onPointerMove={moveDrag} onPointerUp={endDrag} onPointerCancel={endDrag} onLostPointerCapture={endDrag} onClickCapture={event => { if (suppressClick.current && event.detail !== 0) { event.preventDefault(); event.stopPropagation(); } }}>
        <span className="phone-stage-label">{deviceFaces.find(item => item.key === face)?.label}</span>
        <div className="phone-object" style={{ transform: `rotateX(${orientation.x}deg) rotateY(${orientation.y}deg)` }}>
          {Array.from({ length: 19 }, (_, index) => <div key={index} className="phone-shell" aria-hidden="true" style={{ '--depth': `${index - 9}px`, '--shade': `${61 + Math.abs(index - 9) * 3}%` } as CSSProperties}/>)}
          <div className="phone-face phone-front" data-interactive={isVisible('frente')} aria-hidden={!isVisible('frente')}>
            {surface('frente')}{hotspot('tela', 'phone-display', <><span className="phone-wallpaper"/><span className="phone-home-indicator"/></>)}<div className="phone-island" aria-hidden="true"/>{hotspot('face-id', 'phone-faceid', <span/>)}{hotspot('camera-frontal', 'phone-selfie', <span/>)}{markers('frente')}
          </div>
          <div className="phone-face phone-back" data-interactive={isVisible('tras')} aria-hidden={!isVisible('tras')}>
            {surface('tras')}{hotspot('traseira', 'phone-back-glass')}{hotspot('camera-traseira', 'phone-cameras', <><span className="phone-lens"/><span className="phone-lens"/><span className="phone-lens"/><span className="phone-flash"/></>)}<span className="phone-back-emblem" aria-hidden="true"/>{markers('tras')}
          </div>
          <div className="phone-side phone-left" data-interactive={isVisible('esquerda')} aria-hidden={!isVisible('esquerda')}>{surface('esquerda')}{hotspot('botoes-volume', 'phone-volume', <><span/><span/></>)}{markers('esquerda')}</div>
          <div className="phone-side phone-right" data-interactive={isVisible('direita')} aria-hidden={!isVisible('direita')}>{surface('direita')}{hotspot('botao-lateral', 'phone-power', <span/>)}{markers('direita')}</div>
          <div className="phone-end phone-top" data-interactive={isVisible('superior')} aria-hidden={!isVisible('superior')}>{surface('superior')}{markers('superior')}</div>
          <div className="phone-end phone-bottom" data-interactive={isVisible('inferior')} aria-hidden={!isVisible('inferior')}>{surface('inferior')}<span className="phone-grille phone-grille-left" aria-hidden="true"/>{hotspot('conector', 'phone-port', <span/>)}<span className="phone-grille phone-grille-right" aria-hidden="true"/>{markers('inferior')}</div>
        </div>
        <div className="phone-hover-label" aria-live="polite">{dragging ? 'Girando aparelho…' : hovered || 'Arraste para girar · clique para inspecionar'}</div>
      </div>
      <div className="phone-face-controls" aria-label="Escolher face do aparelho">{deviceFaces.map(item => <button type="button" key={item.key} aria-pressed={face === item.key} onClick={() => setFace(item.key)}>{item.label}</button>)}</div>
      <div className="phone-component-shortcuts"><span>Componentes visíveis e bateria interna</span>{currentComponents.map(component => <button type="button" key={component.id} data-defect={selected(component.id)} onClick={() => inspect(component)}>{component.label}<span>{selected(component.id) ? '●' : '+'}</span></button>)}<button type="button" onClick={() => markLocation(face)}>Avaria na face {deviceFaces.find(item => item.key === face)?.label.toLocaleLowerCase('pt-BR')} +</button></div>
    </div>
    <aside className="inspection-findings"><span className="inspection-eyebrow">CONDIÇÃO NA ENTRADA</span><Typography variant="h6" sx={{ mt: .5 }}>Defeitos encontrados</Typography><Typography color="text.secondary" sx={{ fontSize: 12.5, mt: .7, mb: 2 }}>Um local pode ter vários problemas e observações. Riscos e amassados podem ser marcados em todas as faces.</Typography>
      {points.length === 0 ? <div className="inspection-empty"><span className="inspection-empty-icon">+</span><strong>Comece pelo aparelho</strong><p>Gire o modelo, selecione um componente ou ative a marcação livre para indicar o ponto da avaria.</p></div> : <div className="finding-list">{points.map((point, index) => <div key={point.componente ?? `${point.face}-${index}`} className="finding-card"><span className="finding-number">{index + 1}</span><div><strong>{damageLabel(point)}</strong>{pointDefects(point).length > 0 && <ul className="finding-problems">{pointDefects(point).map(item => <li key={item}>{item}</li>)}</ul>}{point.observacao && <p className="finding-observation">{point.observacao}</p>}<div className="finding-actions"><button type="button" onClick={() => { setFace(point.face); openEditor(point, index, deviceComponents.find(item => item.id === point.componente)); }}>Editar</button><button type="button" onClick={() => onChange(points.filter((_, i) => i !== index))}>Remover</button></div></div></div>)}</div>}
      <div className="inspection-legend"><span/><p>Sem marcação: nenhum defeito observado no local. Registre ao menos uma ocorrência para abrir a OS.</p></div>
    </aside></div>
    <Dialog open={editing !== null} onClose={() => setEditing(null)} fullWidth maxWidth="sm"><Box component="form" onSubmit={event => { event.preventDefault(); event.stopPropagation(); saveDefects(); }}>
      <DialogTitle>{editing ? damageLabel(editing.point) : 'Inspeção'}</DialogTitle><DialogContent><Stack spacing={2} sx={{ pt: .5 }}>
        <Typography color="text.secondary" sx={{ fontSize: 13 }}>Selecione todos os problemas encontrados neste local e acrescente os detalhes da inspeção.</Typography>{dialogError && <Alert severity="error">{dialogError}</Alert>}
        <Stack direction="row" flexWrap="wrap" gap={1} aria-label="Problemas encontrados">{options.map(item => <Chip key={item} component="button" type="button" clickable label={item} aria-pressed={defects.includes(item)} variant={defects.includes(item) ? 'filled' : 'outlined'} color={defects.includes(item) ? 'primary' : 'default'} onClick={() => toggleDefect(item)}/>)}</Stack>
        {defects.filter(item => !options.includes(item)).length > 0 && <Stack direction="row" flexWrap="wrap" gap={1}>{defects.filter(item => !options.includes(item)).map(item => <Chip key={item} label={item} onDelete={() => toggleDefect(item)} color="primary" variant="outlined"/>)}</Stack>}
        <Stack direction={{ xs: 'column', sm: 'row' }} gap={1}><TextField label="Outro problema (opcional)" value={customDefect} onChange={event => setCustomDefect(event.target.value)} inputProps={{ maxLength: 60 }} fullWidth size="small" onKeyDown={event => { if (event.key === 'Enter') { event.preventDefault(); addCustom(); } }}/><Button type="button" variant="outlined" onClick={addCustom} disabled={!customDefect.trim()}>Adicionar</Button></Stack>
        <TextField label="Observações deste local" placeholder="Ex.: riscos profundos na borda, canto amassado, peça ausente…" value={observation} onChange={event => setObservation(event.target.value)} inputProps={{ maxLength: 500 }} helperText={`${observation.length}/500 caracteres · pode descrever uma ocorrência sem escolher um problema acima.`} multiline minRows={3} fullWidth/>
      </Stack></DialogContent><DialogActions sx={{ p: 2.5 }}><Button onClick={() => setEditing(null)}>Cancelar</Button><Button variant="contained" type="submit">Salvar registro</Button></DialogActions>
    </Box></Dialog>
  </Box>;
}
