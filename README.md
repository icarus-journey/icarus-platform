# Icarus Platform

Contém a API, o processamento assíncrono, as regras de negócio e a persistência
dos dados transacionais do Icarus.

Loja autenticada: [`05-contrato-da-loja.md`](../icarus-context/docs/implementation-design/platform/05-contrato-da-loja.md)
(RF-06.1 a RF-06.4). Configure o preço da proteção por `Loja__CustoProtecao`
(padrão: 500 pontos). O consumo automático diário é uma entrega separada.

Perfil autenticado: [`04-contrato-de-autenticacao.md`](../icarus-context/docs/implementation-design/platform/04-contrato-de-autenticacao.md).
A consulta existente inclui estudo/trabalho opcionais; `PUT /api/usuarios/{id}/perfil`
atualiza esses quatro campos e `PUT /api/usuarios/{id}/fuso` mantém o fuso IANA.
Cadastro e saldo permanecem protegidos por contratos separados.
