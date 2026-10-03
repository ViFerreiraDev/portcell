import { useEffect, useState } from 'react';
import { Alert, Box, Button, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material';
import { request } from './api';
import { EmptyState, SectionHeader, Icon } from './ui';

type Piece = { id: string; sku: string; descricao: string; saldo: number; estoqueMinimo: number; preco: number };

export function Inventory({ token, profile }: { token: string; profile: string }) {
  const [pieces, setPieces] = useState<Piece[]>([]);
  const [sku, setSku] = useState(''); const [description, setDescription] = useState(''); const [price, setPrice] = useState('');
  const [selected, setSelected] = useState(''); const [quantity, setQuantity] = useState(''); const [reason, setReason] = useState('');
  const [error, setError] = useState('');
  async function load() { try { setPieces(await request<Piece[]>('/api/pecas', token)); setError(''); } catch (error) { setError(String(error)); } }
  useEffect(() => { void load(); }, [token]);
  async function act(path: string, body: unknown) { try { await request(path, token, 'POST', body); await load(); } catch (error) { setError(String(error)); } }
  return <>
    <SectionHeader eyebrow="Materiais e insumos" title="Estoque" description="Cadastre peças, acompanhe saldos e registre entradas." />
    {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
    {['Administrador', 'Gerente'].includes(profile) && <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: 'repeat(2, 1fr)' }, gap: 2.5, mb: 3 }}>
      <Paper component="form" sx={{ p: 3 }} onSubmit={event => { event.preventDefault(); void act('/api/pecas', { sku, descricao: description, custo: 0, preco: Number(price), estoqueMinimo: 0 }); setSku(''); setDescription(''); setPrice(''); }}><Stack spacing={2}><Box><Typography variant="h6">Cadastrar peça</Typography><Typography color="text.secondary" sx={{ fontSize: 13, mt: .4 }}>Adicione um item ao catálogo de estoque.</Typography></Box><TextField label="SKU" value={sku} onChange={event => setSku(event.target.value)} required /><TextField label="Descrição" value={description} onChange={event => setDescription(event.target.value)} required /><TextField label="Preço de venda (R$)" type="number" inputProps={{ min: 0, step: 0.01 }} value={price} onChange={event => setPrice(event.target.value)} required /><Box><Button type="submit" variant="contained" startIcon={<Icon name="plus" size={16}/>}>Cadastrar peça</Button></Box></Stack></Paper>
      <Paper component="form" sx={{ p: 3 }} onSubmit={event => { event.preventDefault(); void act(`/api/pecas/${selected}/movimentos`, { tipo: 'Entrada', quantidade: Number(quantity), justificativa: reason }); setQuantity(''); setReason(''); }}><Stack spacing={2}><Box><Typography variant="h6">Registrar entrada</Typography><Typography color="text.secondary" sx={{ fontSize: 13, mt: .4 }}>Atualize o saldo de uma peça cadastrada.</Typography></Box><TextField select label="Peça" value={selected} onChange={event => setSelected(event.target.value)} required>{pieces.map(x => <MenuItem key={x.id} value={x.id}>{x.sku} · {x.descricao}</MenuItem>)}</TextField><TextField label="Quantidade" type="number" inputProps={{ min: 0.001, step: 0.001 }} value={quantity} onChange={event => setQuantity(event.target.value)} required /><TextField label="Documento / justificativa" value={reason} onChange={event => setReason(event.target.value)} required /><Box><Button type="submit" variant="contained">Registrar entrada</Button></Box></Stack></Paper>
    </Box>}
    <Typography variant="h6" sx={{ mb: 2 }}>Peças cadastradas <Box component="span" sx={{ color: 'text.secondary', fontSize: 13, fontWeight: 500, ml: 1 }}>({pieces.length})</Box></Typography>
    {pieces.length === 0 ? <EmptyState icon="stock" title="Nenhuma peça cadastrada" description="Cadastre peças para controlar entradas, consumo e alertas de estoque." /> : <Paper sx={{ overflow: 'hidden' }}>{pieces.map((x, i) => <Stack key={x.id} direction="row" spacing={2} alignItems="center" sx={{ px: { xs: 2, sm: 3 }, py: 2, borderBottom: i < pieces.length - 1 ? '1px solid #edf0f1' : 0 }}><Box sx={{ width: 40, height: 40, flexShrink: 0, borderRadius: 2, bgcolor: '#eaf4f3', color: 'primary.main', display: 'grid', placeItems: 'center' }}><Icon name="stock" size={20}/></Box><Box sx={{ minWidth: 0, flex: 1 }}><Typography sx={{ fontWeight: 730 }}>{x.descricao}</Typography><Typography color="text.secondary" sx={{ fontSize: 12.5 }}>SKU {x.sku} · Mínimo {x.estoqueMinimo}</Typography></Box><Box sx={{ textAlign: 'right' }}><Typography sx={{ fontWeight: 800 }}>{x.saldo}</Typography>{x.saldo <= x.estoqueMinimo && <Typography sx={{ color: 'warning.main', fontSize: 11, fontWeight: 750 }}>Estoque mínimo</Typography>}</Box></Stack>)}</Paper>}
  </>;
}
