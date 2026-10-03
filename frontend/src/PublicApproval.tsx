import { useEffect, useState } from 'react';
import { Alert, Box, Button, Checkbox, Container, FormControlLabel, Paper, Stack, Typography } from '@mui/material';
import { request } from './api';
import { Brand, Icon } from './ui';

type Quote = {
  numeroOs: number; aparelho: string; diagnostico: string; versao: number;
  itens: { tipo: string; descricao: string; quantidade: number; valorUnitario: number }[];
  subtotal: number; desconto: number; total: number; condicoes?: string; prazoEstimado: string; validoAte: string;
};
const money = (value: number) => value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

export function PublicApproval({ token }: { token: string }) {
  const [quote, setQuote] = useState<Quote | null>(null);
  const [accepted, setAccepted] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState('');
  useEffect(() => { request<Quote>(`/api/public/autorizacoes/${encodeURIComponent(token)}`).then(setQuote).catch(error => setError(String(error))); }, [token]);
  async function decide(approved: boolean) {
    try {
      const response = await request<{ resultado: string }>(`/api/public/autorizacoes/${encodeURIComponent(token)}/${approved ? 'aprovar' : 'recusar'}`, undefined, 'POST', { confirmouLeitura: accepted });
      setResult(response.resultado);
      setError('');
    } catch (error) { setError(String(error)); }
  }
  return <Box sx={{ minHeight: '100vh', bgcolor: '#f5f8f8' }}>
    <Box sx={{ bgcolor: '#132f40', py: 2.5 }}><Container maxWidth="md"><Brand light /></Container></Box>
    <Container maxWidth="md" sx={{ py: { xs: 3, sm: 5 } }}><Stack spacing={3}>
      <Box><Typography sx={{ color: 'primary.main', fontSize: 11, fontWeight: 800, letterSpacing: '.13em', mb: 1 }}>PORTCELL · AUTORIZAÇÃO</Typography><Typography variant="h4">Revise seu orçamento</Typography><Typography color="text.secondary" sx={{ mt: .8 }}>Confira os detalhes abaixo antes de tomar uma decisão.</Typography></Box>
      {error && <Alert severity="error">{error}</Alert>}
      {result && <Paper sx={{ p: 4, textAlign: 'center' }}><Box sx={{ color: 'success.main', mb: 1.5 }}><Icon name="check" size={32}/></Box><Typography variant="h5">Resposta registrada</Typography><Typography color="text.secondary" sx={{ mt: 1 }}>Sua resposta foi registrada: {result}.</Typography></Paper>}
      {quote && !result && <Paper sx={{ overflow: 'hidden' }}>
        <Box sx={{ p: { xs: 2.5, sm: 3.5 }, borderBottom: '1px solid #e5ebec' }}><Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" gap={1}><Box><Typography sx={{ color: 'text.secondary', fontSize: 12, fontWeight: 750 }}>ORDEM DE SERVIÇO #{quote.numeroOs}</Typography><Typography variant="h5" sx={{ mt: .5 }}>{quote.aparelho}</Typography></Box><Typography sx={{ color: 'primary.main', fontSize: 13, fontWeight: 700 }}>Orçamento v{quote.versao}</Typography></Stack></Box>
        <Box sx={{ p: { xs: 2.5, sm: 3.5 } }}><Typography sx={{ color: 'text.secondary', fontSize: 11, fontWeight: 800, letterSpacing: '.08em' }}>DIAGNÓSTICO</Typography><Typography sx={{ mt: .8, mb: 3 }}>{quote.diagnostico}</Typography>
          <Typography sx={{ color: 'text.secondary', fontSize: 11, fontWeight: 800, letterSpacing: '.08em', mb: 1.2 }}>ITENS DO ORÇAMENTO</Typography>
          {quote.itens.map((item, index) => <Stack key={index} direction="row" justifyContent="space-between" gap={2} sx={{ py: 1.5, borderBottom: '1px solid #edf0f1' }}><Box><Typography sx={{ fontWeight: 650 }}>{item.descricao}</Typography><Typography color="text.secondary" sx={{ fontSize: 12 }}>{item.quantidade} × {money(item.valorUnitario)}</Typography></Box><Typography sx={{ fontWeight: 700, flexShrink: 0 }}>{money(item.quantidade * item.valorUnitario)}</Typography></Stack>)}
          <Stack spacing={1} sx={{ mt: 2 }}><Stack direction="row" justifyContent="space-between"><Typography color="text.secondary">Subtotal</Typography><Typography>{money(quote.subtotal)}</Typography></Stack><Stack direction="row" justifyContent="space-between"><Typography color="text.secondary">Desconto</Typography><Typography>{money(quote.desconto)}</Typography></Stack></Stack>
        </Box>
        <Box sx={{ px: { xs: 2.5, sm: 3.5 }, py: 2.5, bgcolor: '#eaf5f4', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}><Typography sx={{ fontWeight: 750 }}>Total do orçamento</Typography><Typography sx={{ color: 'primary.dark', fontSize: 25, fontWeight: 850 }}>{money(quote.total)}</Typography></Box>
        <Box sx={{ p: { xs: 2.5, sm: 3.5 } }}><Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2.5 }}><Box sx={{ flex: 1 }}><Typography color="text.secondary" sx={{ fontSize: 12 }}>Prazo estimado</Typography><Typography sx={{ fontWeight: 650 }}>{quote.prazoEstimado}</Typography></Box><Box sx={{ flex: 1 }}><Typography color="text.secondary" sx={{ fontSize: 12 }}>Válido até</Typography><Typography sx={{ fontWeight: 650 }}>{new Date(quote.validoAte).toLocaleString('pt-BR')}</Typography></Box></Stack>
          {quote.condicoes && <Box sx={{ p: 2, bgcolor: '#f7f9fa', borderRadius: 2, mb: 2.5 }}><Typography sx={{ fontSize: 12, fontWeight: 750, mb: .4 }}>Condições</Typography><Typography color="text.secondary" sx={{ fontSize: 13 }}>{quote.condicoes}</Typography></Box>}
          <FormControlLabel control={<Checkbox checked={accepted} onChange={event => setAccepted(event.target.checked)} />} label="Li e compreendi o orçamento apresentado." />
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ mt: 2 }}><Button variant="contained" disabled={!accepted} onClick={() => decide(true)}>Autorizar reparo</Button><Button variant="outlined" color="error" disabled={!accepted} onClick={() => decide(false)}>Recusar orçamento</Button></Stack>
        </Box>
      </Paper>}
    </Stack></Container>
  </Box>;
}
