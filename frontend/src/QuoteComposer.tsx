import { useEffect, useState } from 'react';
import { Alert, Box, Button, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { request, type Orcamento } from './api';
import type { Service } from './ServiceAdministration';

type Item = { tipo: string; descricao: string; quantidade: number; valorUnitario: number };

export function QuoteComposer({ orderId, token, onCreated }: { orderId: string; token: string; onCreated: (quote: Orcamento) => void }) {
  const [items, setItems] = useState<Item[]>([]);
  const [type, setType] = useState('Servico'); const [description, setDescription] = useState(''); const [quantity, setQuantity] = useState('1'); const [price, setPrice] = useState('');
  const [discount, setDiscount] = useState('0'); const [days, setDays] = useState('7'); const [deadline, setDeadline] = useState(''); const [terms, setTerms] = useState('');
  const [error, setError] = useState('');
  const [services, setServices] = useState<Service[]>([]);
  useEffect(() => { request<Service[]>('/api/servicos', token).then(setServices).catch(() => setServices([])); }, [token]);
  const subtotal = items.reduce((sum, item) => sum + item.quantidade * item.valorUnitario, 0);
  function addItem() {
    if (!description.trim() || Number(quantity) <= 0 || Number(price) < 0 || !price) return;
    setItems([...items, { tipo: type, descricao: description.trim(), quantidade: Number(quantity), valorUnitario: Number(price) }]);
    setDescription(''); setQuantity('1'); setPrice('');
  }
  async function create(event: React.FormEvent) {
    event.preventDefault();
    try {
      const quote = await request<Orcamento>(`/api/ordens-servico/${orderId}/orcamentos`, token, 'POST',
        { itens: items, desconto: Number(discount), validoAte: new Date(Date.now() + Number(days) * 86400000).toISOString(), prazoEstimado: deadline, condicoes: terms });
      setItems([]); setDiscount('0'); setDeadline(''); setTerms(''); setError(''); onCreated(quote);
    } catch (error) { setError(String(error)); }
  }
  return <Box component="form" onSubmit={create}><Stack spacing={1}>
    <Typography variant="h6">Nova versão de orçamento</Typography>{error && <Alert severity="error">{error}</Alert>}
    {services.length > 0 && <TextField select label="Usar serviço do catálogo" value="" onChange={event => { const selected = services.find(x => x.id === event.target.value); if (selected) { setType('Servico'); setDescription(selected.nome); setPrice(String(selected.precoPadrao)); } }}><MenuItem value="">Selecione para preencher</MenuItem>{services.map(service => <MenuItem key={service.id} value={service.id}>{service.nome}</MenuItem>)}</TextField>}
    <TextField select label="Tipo do item" value={type} onChange={event => setType(event.target.value)}><MenuItem value="Servico">Serviço</MenuItem><MenuItem value="Peca">Peça</MenuItem></TextField>
    <TextField label="Descrição" value={description} onChange={event => setDescription(event.target.value)} /><TextField label="Quantidade" type="number" inputProps={{ min: 0.001, step: 0.001 }} value={quantity} onChange={event => setQuantity(event.target.value)} /><TextField label="Valor unitário (R$)" type="number" inputProps={{ min: 0, step: 0.01 }} value={price} onChange={event => setPrice(event.target.value)} />
    <Button type="button" onClick={addItem}>Adicionar item</Button>
    {items.map((item, index) => <Stack key={index} direction="row" spacing={1} alignItems="center"><Typography sx={{ flexGrow: 1 }}>{item.tipo}: {item.descricao} · {item.quantidade} × R$ {item.valorUnitario.toFixed(2)}</Typography><Button type="button" color="error" onClick={() => setItems(items.filter((_, i) => i !== index))}>Remover</Button></Stack>)}
    <Typography>Subtotal: R$ {subtotal.toFixed(2)}</Typography><TextField label="Desconto (R$)" type="number" inputProps={{ min: 0, max: subtotal, step: 0.01 }} value={discount} onChange={event => setDiscount(event.target.value)} /><TextField label="Validade em dias" type="number" inputProps={{ min: 1, max: 365 }} value={days} onChange={event => setDays(event.target.value)} /><TextField label="Prazo estimado de execução" required value={deadline} onChange={event => setDeadline(event.target.value)} placeholder="Ex.: 2 dias úteis após aprovação" /><TextField label="Condições" multiline value={terms} onChange={event => setTerms(event.target.value)} />
    <Button type="submit" variant="contained" disabled={items.length === 0}>Apresentar orçamento</Button>
  </Stack></Box>;
}
