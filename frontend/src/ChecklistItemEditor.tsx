import { useEffect, useState } from 'react';
import { Alert, Button, Checkbox, FormControlLabel, Paper, Stack, TextField, Typography } from '@mui/material';
import type { ChecklistItem } from './api';

export function ChecklistItemEditor({ item, onSave }: { item: ChecklistItem; onSave: (value: ChecklistItem) => Promise<void> }) {
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(item.nome);
  const [order, setOrder] = useState(String(item.ordem));
  const [options, setOptions] = useState(item.opcoes.join(', '));
  const [notes, setNotes] = useState(item.permiteObservacao);
  const [active, setActive] = useState(item.ativo);
  const [error, setError] = useState('');
  useEffect(() => { setName(item.nome); setOrder(String(item.ordem)); setOptions(item.opcoes.join(', ')); setNotes(item.permiteObservacao); setActive(item.ativo); }, [item]);
  async function save(event: React.FormEvent) {
    event.preventDefault();
    try {
      await onSave({ ...item, nome: name.trim(), ordem: Number(order), opcoes: options.split(',').map(x => x.trim()).filter(Boolean), permiteObservacao: notes, ativo: active });
      setEditing(false); setError('');
    } catch (error) { setError(String(error)); }
  }
  return <Paper variant="outlined" sx={{ p: 1 }}>
    {!editing ? <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} alignItems={{ sm: 'center' }}><Typography sx={{ flexGrow: 1 }}>{item.ordem}. {item.nome} · {item.ativo ? 'Ativo' : 'Inativo'}</Typography><Button onClick={() => setEditing(true)}>Editar</Button></Stack>
      : <Stack component="form" onSubmit={save} spacing={1}>{error && <Alert severity="error">{error}</Alert>}
        <TextField label="Item" required value={name} onChange={event => setName(event.target.value)} />
        <TextField label="Ordem" type="number" inputProps={{ min: 0 }} required value={order} onChange={event => setOrder(event.target.value)} />
        <TextField label="Opções separadas por vírgula" required value={options} onChange={event => setOptions(event.target.value)} />
        <FormControlLabel control={<Checkbox checked={notes} onChange={event => setNotes(event.target.checked)} />} label="Permitir observação" />
        <FormControlLabel control={<Checkbox checked={active} onChange={event => setActive(event.target.checked)} />} label="Ativo" />
        <Stack direction="row" spacing={1}><Button type="submit">Salvar</Button><Button type="button" onClick={() => setEditing(false)}>Cancelar</Button></Stack>
      </Stack>}
  </Paper>;
}
