import { useState, type FormEvent } from 'react';
import { Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle, Divider, InputAdornment, Paper, Stack, Tab, Tabs, TextField, Typography } from '@mui/material';
import type { Service } from './ServiceAdministration';
import { Icon } from './ui';
import './intake-budget.css';

export type Piece = { id: string; sku: string; descricao: string; preco?: number; saldo: number };
export type QuoteItem = { tipo: 'Servico' | 'Peca'; descricao: string; quantidade: number; valorUnitario: number; pecaId?: string };
export type BudgetDraft = { items: QuoteItem[]; discount: string; validDays: string; deadline: string; terms: string };
const money = (amount: number) => amount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
export const itemTotal = (item: QuoteItem) => Math.round((item.quantidade * item.valorUnitario + Number.EPSILON) * 100) / 100;

export function IntakeBudget({ value, onChange, services, pieces }: {
  value: BudgetDraft; onChange: (value: BudgetDraft) => void; services: Service[]; pieces: Piece[];
}) {
  const [open, setOpen] = useState(false); const [kind, setKind] = useState<'Servico' | 'Peca'>('Servico');
  const [query, setQuery] = useState(''); const [editingIndex, setEditingIndex] = useState(-1);
  const [draft, setDraft] = useState<QuoteItem | null>(null); const [quantity, setQuantity] = useState('1'); const [price, setPrice] = useState('');
  const [error, setError] = useState('');
  const subtotal = value.items.reduce((sum, item) => sum + itemTotal(item), 0);
  const discount = Number(value.discount || 0); const total = subtotal - discount;
  const update = (patch: Partial<BudgetDraft>) => onChange({ ...value, ...patch });
  const normalized = query.trim().toLocaleLowerCase('pt-BR');
  const catalog: (QuoteItem & { id: string; detail: string })[] = kind === 'Peca'
    ? pieces.map(item => ({ id: item.id, tipo: 'Peca', descricao: item.descricao, quantidade: 1, valorUnitario: item.preco ?? 0, pecaId: item.id, detail: `${item.sku} · ${item.saldo} em estoque` }))
    : services.filter(item => item.ativo).map(item => ({ id: item.id, tipo: 'Servico', descricao: item.nome, quantidade: 1, valorUnitario: item.precoPadrao, detail: 'Serviço do catálogo' }));
  const filtered = catalog.filter(item => `${item.descricao} ${item.detail}`.toLocaleLowerCase('pt-BR').includes(normalized));

  function start(index = -1) {
    setEditingIndex(index); setQuery(''); setError('');
    const item = index >= 0 ? value.items[index] : null;
    setDraft(item); setKind(item?.tipo ?? 'Servico'); setQuantity(String(item?.quantidade ?? 1)); setPrice(item ? String(item.valorUnitario) : ''); setOpen(true);
  }
  function choose(item: QuoteItem) { setDraft(item); setQuantity(String(item.quantidade)); setPrice(String(item.valorUnitario)); setError(''); }
  function save(event: FormEvent) {
    event.preventDefault(); event.stopPropagation();
    if (!draft) return;
    const amount = Number(quantity); const unitPrice = Number(price);
    if (!draft.descricao.trim() || draft.descricao.length > 300 || !quantity || !price || !Number.isFinite(amount) || amount <= 0 || amount > 10000 || !Number.isFinite(unitPrice) || unitPrice < 0 || unitPrice > 1000000) { setError('Confira descrição, quantidade e valor unitário.'); return; }
    const item: QuoteItem = { tipo: draft.tipo, descricao: draft.descricao.trim(), quantidade: amount, valorUnitario: unitPrice, ...(draft.pecaId ? { pecaId: draft.pecaId } : {}) };
    const items = [...value.items];
    if (editingIndex >= 0) items[editingIndex] = item;
    else {
      if (items.length >= 10) { setError('O orçamento comporta até 10 itens. Edite um item existente.'); return; }
      items.push(item);
    }
    update({ items }); setOpen(false);
  }
  return <>
    <div className="budget-layout">
      <Paper className="budget-items-panel">
        <div className="budget-panel-heading"><div><Typography variant="h6">Serviços e peças</Typography><Typography color="text.secondary" fontSize={13} sx={{ mt: .5 }}>Monte o orçamento que será apresentado ao cliente.</Typography></div><Chip size="small" label={`${value.items.length}/10 itens`} variant="outlined"/></div>
        {value.items.length ? <div className="budget-items">{value.items.map((item, index) => <div className="budget-item" key={index}>
          <span className={`budget-item-icon ${item.tipo === 'Peca' ? 'is-piece' : ''}`}><Icon name={item.tipo === 'Peca' ? 'stock' : 'settings'} size={20}/></span>
          <div className="budget-item-description"><span className="budget-item-type">{item.tipo === 'Peca' ? 'PEÇA' : 'SERVIÇO'}</span><strong>{item.descricao}</strong><span>{item.quantidade} × {money(item.valorUnitario)}</span><div className="budget-item-actions"><button type="button" onClick={() => start(index)} aria-label={`Editar ${item.descricao}`}>Editar</button><button type="button" onClick={() => update({ items: value.items.filter((_, position) => position !== index) })} aria-label={`Remover ${item.descricao}`}>Remover</button></div></div>
          <strong className="budget-item-total">{money(itemTotal(item))}</strong>
        </div>)}</div> : <div className="budget-empty"><span><Icon name="orders" size={30}/></span><Typography variant="h6">O que será feito no aparelho?</Typography><Typography color="text.secondary" fontSize={13}>Adicione serviços do catálogo, peças ou um serviço personalizado.</Typography></div>}
        <Button type="button" variant={value.items.length ? 'outlined' : 'contained'} fullWidth startIcon={<Icon name="plus" size={18}/>} disabled={value.items.length >= 10} onClick={() => start()} sx={{ minHeight: 48 }}>Adicionar serviço ou peça</Button>
        <div className="budget-stock-note"><Icon name="stock" size={16}/><span>As peças serão retiradas do estoque após a aprovação.</span></div>
        <Divider sx={{ my: 2.5 }}/><TextField label="Condições do orçamento (opcional)" placeholder="Ex.: forma de pagamento e condições combinadas" value={value.terms} onChange={e => update({ terms: e.target.value })} inputProps={{ maxLength: 2000 }} multiline minRows={2} fullWidth/>
      </Paper>
      <Paper className="budget-summary">
        <span className="budget-summary-eyebrow">RESUMO DO ORÇAMENTO</span>
        <div className="budget-summary-line"><span>Subtotal</span><strong>{money(subtotal)}</strong></div>
        <TextField size="small" label="Desconto" value={value.discount} type="number" onChange={e => update({ discount: e.target.value })} error={!Number.isFinite(discount) || discount < 0 || discount > subtotal} helperText={discount > subtotal ? 'O desconto não pode superar o subtotal.' : undefined} inputProps={{ min: 0, max: subtotal, step: .01 }} InputProps={{ startAdornment: <InputAdornment position="start">R$</InputAdornment> }} fullWidth/>
        <div className="budget-grand-total"><span>Total para o cliente</span><strong>{money(Math.max(0, total || 0))}</strong></div>
        <Divider sx={{ mb: 2.5 }}/>
        <Stack spacing={2}><TextField label="Prazo de execução" placeholder="Ex.: 2 dias úteis" value={value.deadline} onChange={e => update({ deadline: e.target.value })} required inputProps={{ maxLength: 120 }} helperText="Contado após a aprovação." fullWidth size="small"/><TextField label="Validade do orçamento" type="number" value={value.validDays} onChange={e => update({ validDays: e.target.value })} required inputProps={{ min: 1, max: 365, step: 1 }} InputProps={{ endAdornment: <InputAdornment position="end">dias</InputAdornment> }} fullWidth size="small"/></Stack>
      </Paper>
    </div>
    <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm"><Box component="form" onSubmit={save}>
      <DialogTitle>{editingIndex >= 0 ? 'Editar item do orçamento' : 'Adicionar ao orçamento'}<Typography color="text.secondary" variant="body2" sx={{ mt: .5 }}>{draft ? 'Confira os detalhes antes de incluir o item.' : 'Encontre um serviço ou peça para este atendimento.'}</Typography></DialogTitle>
      <DialogContent>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        {!draft ? <><Tabs value={kind} onChange={(_, selectedKind: 'Servico' | 'Peca') => { setKind(selectedKind); setQuery(''); }} variant="fullWidth" aria-label="Tipo de item do orçamento" sx={{ mb: 2 }}><Tab value="Servico" label="Serviços"/><Tab value="Peca" label="Peças do estoque"/></Tabs>
          <TextField placeholder={kind === 'Servico' ? 'Buscar serviço…' : 'Buscar peça por nome ou código…'} value={query} onChange={e => setQuery(e.target.value)} fullWidth autoFocus size="small" InputProps={{ startAdornment: <InputAdornment position="start"><Icon name="search" size={18}/></InputAdornment> }}/>
          <div className="budget-catalog">{filtered.length ? filtered.map(item => <button type="button" className="budget-catalog-item" key={item.id} onClick={() => choose(item)}><span className="budget-catalog-copy"><strong>{item.descricao}</strong><small>{item.detail}</small></span><span className="budget-catalog-price">{money(item.valorUnitario)}<Icon name="plus" size={17}/></span></button>) : <p className="budget-catalog-empty">{query ? 'Nenhum item encontrado para esta busca.' : kind === 'Peca' ? 'Nenhuma peça cadastrada no estoque.' : 'Seu catálogo de serviços ainda está vazio.'}</p>}</div>
          {kind === 'Servico' && <Button type="button" variant="outlined" fullWidth startIcon={<Icon name="plus" size={16}/>} onClick={() => { setDraft({ tipo: 'Servico', descricao: query.trim(), quantidade: 1, valorUnitario: 0 }); setPrice(''); }}>Criar serviço personalizado</Button>}
        </> : <Stack spacing={2.5} sx={{ pt: 1 }}>
          {editingIndex < 0 && <Button type="button" size="small" sx={{ alignSelf: 'flex-start' }} onClick={() => setDraft(null)}>← Voltar ao catálogo</Button>}
          <Chip label={draft.tipo === 'Peca' ? 'Peça do estoque' : 'Serviço'} size="small" color="primary" variant="outlined" sx={{ alignSelf: 'flex-start' }}/>
          <TextField label="Descrição do item" value={draft.descricao} onChange={e => setDraft({ ...draft, descricao: e.target.value })} required disabled={draft.tipo === 'Peca'} inputProps={{ maxLength: 300 }} autoFocus fullWidth/>
          <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}><TextField label="Quantidade" type="number" value={quantity} onChange={e => setQuantity(e.target.value)} required inputProps={{ min: .001, max: 10000, step: .001 }}/><TextField label="Valor unitário" type="number" value={price} onChange={e => setPrice(e.target.value)} required inputProps={{ min: 0, max: 1000000, step: .01 }} InputProps={{ startAdornment: <InputAdornment position="start">R$</InputAdornment> }}/></Box>
          <div className="budget-item-preview"><span>Total deste item</span><strong>{money(Number(quantity) * Number(price) || 0)}</strong></div>
        </Stack>}
      </DialogContent><DialogActions sx={{ p: 2.5 }}><Button onClick={() => setOpen(false)}>Cancelar</Button>{draft && <Button type="submit" variant="contained" startIcon={<Icon name={editingIndex >= 0 ? 'check' : 'plus'} size={17}/>}>{editingIndex >= 0 ? 'Salvar alterações' : 'Adicionar ao orçamento'}</Button>}</DialogActions>
    </Box></Dialog>
  </>;
}
