import { useState } from 'react';
import { Alert, Box, Button, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { request, type Orcamento } from './api';

export function ManualAuthorization({ orderId, quote, token, onDecided }: { orderId: string; quote: Orcamento; token: string; onDecided: (approved: boolean) => void }) {
  const [channel, setChannel] = useState('WhatsApp'); const [evidence, setEvidence] = useState(''); const [error, setError] = useState('');
  async function decide(approved: boolean) {
    try { await request(`/api/ordens-servico/${orderId}/autorizacoes`, token, 'POST', { orcamentoVersaoId: quote.id, canal: channel, aprovado: approved, evidencia: evidence }); setError(''); onDecided(approved); }
    catch (error) { setError(String(error)); }
  }
  return <Box><Stack spacing={1}><Typography variant="h6">Registrar resposta recebida</Typography>{error && <Alert severity="error">{error}</Alert>}
    <TextField select label="Canal" value={channel} onChange={event => setChannel(event.target.value)}>{['WhatsApp', 'Email', 'Assinatura', 'Outro'].map(x => <MenuItem key={x} value={x}>{x}</MenuItem>)}</TextField>
    <TextField label="Referência da evidência" helperText="Ex.: data e identificação da conversa ou documento assinado" value={evidence} onChange={event => setEvidence(event.target.value)} required />
    <Stack direction="row" spacing={1}><Button variant="contained" disabled={!evidence.trim()} onClick={() => decide(true)}>Registrar aprovação</Button><Button color="error" disabled={!evidence.trim()} onClick={() => decide(false)}>Registrar recusa</Button></Stack>
  </Stack></Box>;
}
