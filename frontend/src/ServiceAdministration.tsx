import { useEffect, useState } from 'react';
import { Alert, Button, Checkbox, FormControlLabel, Paper, Stack, TextField, Typography } from '@mui/material';
import { request } from './api';

export type Service = { id: string; nome: string; precoPadrao: number; garantiaDias: number; condicoesGarantia: string; ativo: boolean };
type ServiceFields = Omit<Service, 'id'>;
const empty: ServiceFields = { nome: '', precoPadrao: 0, garantiaDias: 90, condicoesGarantia: 'Garantia do serviço realizado.', ativo: true };

function ServiceForm({ value, label, onSave }: { value: ServiceFields; label: string; onSave: (value: ServiceFields) => Promise<void> }) {
  const [form, setForm] = useState(value);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  useEffect(() => setForm(value), [value.nome, value.precoPadrao, value.garantiaDias, value.condicoesGarantia, value.ativo]);
  async function submit(event: React.FormEvent) {
    event.preventDefault(); setSaving(true);
    try { await onSave(form); setError(''); if (label === 'Cadastrar serviço') setForm(empty); }
    catch (error) { setError(String(error)); }
    finally { setSaving(false); }
  }
  return <Stack component="form" onSubmit={submit} spacing={1} sx={{ py: 1 }}>
    {error && <Alert severity="error">{error}</Alert>}
    <TextField label="Serviço" required value={form.nome} onChange={event => setForm({ ...form, nome: event.target.value })} />
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
      <TextField label="Preço padrão (R$)" type="number" inputProps={{ min: 0, step: 0.01 }} required value={form.precoPadrao} onChange={event => setForm({ ...form, precoPadrao: Number(event.target.value) })} />
      <TextField label="Garantia (dias)" type="number" inputProps={{ min: 1, max: 3650 }} required value={form.garantiaDias} onChange={event => setForm({ ...form, garantiaDias: Number(event.target.value) })} />
    </Stack>
    <TextField label="Condições de garantia" required multiline value={form.condicoesGarantia} onChange={event => setForm({ ...form, condicoesGarantia: event.target.value })} />
    <FormControlLabel control={<Checkbox checked={form.ativo} onChange={event => setForm({ ...form, ativo: event.target.checked })} />} label="Ativo" />
    <Button type="submit" disabled={saving}>{label}</Button>
  </Stack>;
}

export function ServiceAdministration({ token }: { token: string }) {
  const [services, setServices] = useState<Service[]>([]);
  const [error, setError] = useState('');
  async function load() { try { setServices(await request<Service[]>('/api/servicos?todos=true', token)); setError(''); } catch (error) { setError(String(error)); } }
  useEffect(() => { void load(); }, [token]);
  return <Paper sx={{ p: 3 }}><Stack spacing={1}>
    <Typography variant="h6">Catálogo de serviços e garantias</Typography>
    {error && <Alert severity="error">{error}</Alert>}
    <ServiceForm value={empty} label="Cadastrar serviço" onSave={async value => { await request('/api/servicos', token, 'POST', value); await load(); }} />
    {services.map(service => <Paper key={service.id} variant="outlined" sx={{ p: 2 }}><Typography fontWeight={600}>{service.nome}</Typography>
      <ServiceForm value={service} label="Salvar alterações" onSave={async value => { await request(`/api/servicos/${service.id}`, token, 'PUT', value); await load(); }} />
    </Paper>)}
  </Stack></Paper>;
}
