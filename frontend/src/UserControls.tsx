import { useState } from 'react';
import { Alert, Button, Paper, Stack, TextField, Typography } from '@mui/material';
import { request } from './api';

export type ManagedUser = { id: string; nome: string; email: string; perfil: string; ativo: boolean };

export function UserControls({ user, token, onChanged }: { user: ManagedUser; token: string; onChanged: () => Promise<void> }) {
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  async function act(path: string, body?: unknown) {
    setSaving(true);
    try { await request(path, token, 'PATCH', body); await onChanged(); setError(''); }
    catch (error) { setError(String(error)); }
    finally { setSaving(false); }
  }
  async function reset() {
    setSaving(true);
    try { await request(`/api/usuarios/${user.id}/redefinir-senha`, token, 'POST', { novaSenha: password }); setPassword(''); setError(''); await onChanged(); }
    catch (error) { setError(String(error)); }
    finally { setSaving(false); }
  }
  return <Paper variant="outlined" sx={{ p: 2 }}><Stack spacing={1}>
    <Typography>{user.nome} · {user.email} · {user.perfil} · {user.ativo ? 'Ativo' : 'Inativo'}</Typography>
    {error && <Alert severity="error">{error}</Alert>}
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
      <Button disabled={saving} onClick={() => void act(`/api/usuarios/${user.id}/${user.ativo ? 'desativar' : 'ativar'}`)}>{user.ativo ? 'Desativar' : 'Ativar'}</Button>
      <TextField label="Nova senha (mínimo 12 caracteres)" type="password" autoComplete="new-password" size="small" value={password} onChange={event => setPassword(event.target.value)} />
      <Button disabled={saving || password.length < 12} onClick={() => void reset()}>Redefinir senha</Button>
    </Stack>
  </Stack></Paper>;
}
