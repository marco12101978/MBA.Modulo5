## Funcionalidade 30%

Avalie se o projeto atende a todos os requisitos funcionais definidos.
* Será revisado na avalição final.

## Qualidade do Código 20%

Considere clareza, organização e uso de padrões de codificação.
* Pontos positivos:
  - Divisão clara de contextos (Alunos, Conteúdo, etc).
  - Bom início de cobertura de código com testes. Está em +40%, o que é um bom começo.
  - Gostei do uso do termo "Matricula". Faz muito sentido.
  - Boa organização dos comandos, handlers e validadores.
  - Pouquissimos warnings durante a compilação. Tente ir à zero.

* Comentários e recomendações:

### Core
 * As Domain Validations podem ser substituidas por FluentValidation e Data Annotations, que é mais flexível e mantém as entidades mais limpas. Evite "reinventar a roda".
 * Renomear `CommandRaiz` para `RaizCommand`, que segue a convenção de nomenclatura e fica correto em inglês.
 * Recomendo renomear `EhValido()` para `EstaValido()`, que é mais claro e correto em português.
 * Em `IRepository<>`, considere adotar um método `Salvar()` ao invés de expor o `UnitOfWork` diretamente.

### Domain
 * Em `Aluno`:
   * Alguns métodos existe validação, mas não em outros. E as validação não usam o validador comum `ValidarInegridadeXxxx()`. Isso pode levar a inconsistências. Sugiro padronizar o uso de validações, **eu prefiro** validar dentro de cada métodos, mas isso é preferencia minha, façam como preferirem.
   * O método `ObterMatriculaPorCursoId()` solta excessão caso não exista, o que pode não ser desejado. Talvez retornar `null` seja melhor.
   * Existe métodos duplicados, como `ObterMatriculaPorCursoId()` e `ObterMatriculaCursoPeloId()`. Recomendo usar de sobrecarga. Ex: `ObterMatriculaPorCursoId(Guid cursoId)` e `ObterMatricula(Guid? cursoId = null, Guid? matriculaId = null)`.
   * Utilize `var` ao invés de tipos explícitos quando o tipo é óbvio. Ex: `var matricula = ...` ao inves de `MatriculaCurso matriculaCurso = ...`
 * Em `MatriculaCurso`:
   * Pode se chamar apenas `Matricula`, o que é mais simples e claro.
   * Algo para pensar: os cálculos de total de aulas, carga horária, e etc, podem ser feitas quando um novo histórico é adicionado. Assim é possível obter a matrícula sem o histórico a ainda sim ter os cálculos corretos.
 * O enumerador `EstadoMatriculaCursoEnum`, remover o sufixo `Enum`, que é redundante e desnecessario. Ele não deve ficar em uma pasta separa, mas junto com a entidade `Matricula`.
 * Em `IAlunoRepository`, considerem o uso de sobrecarga ao invés de criar métodos com nomes diferentes para o mesmo propósito. Ex: `Obter(Guid alunoId)` e `Obter(string email)`, ao invés de `ObterPorId` e `ObterPorEmail`.
 * Separar os repositórios em seus sub-contextos. `AlunoRepository` , `MatriculaRepository`, `CertificadoRepository`. em código, quanto menor, melhor.
 * Evite essa pasta `Interfaces`, que é um anti-padrão. Coloque as interfaces junto com suas implementações ou em pastas específicas. Ex: `Repositories/IAlunoRepository.cs` e `Repositories/IMatriculaRepository.cs`.
 * Se `HistoricoAprendizado` é um `Value Object`, ele não deve ter um ID. IDs são para entidades.

### Application
 * Nome de pastas devem ser no plural. Ex: `Services`, `Commands`, `Queries`, `Integrations`.
 * Nos _handlers_, percebi que está sendo persistido um `Guid` como um `_raizAgregacao`. Como o handler está registrado como `Scoped`, isso pode levar a problemas de concorrência. Lendo o código eu não ví razão para um handler ser `statefull`. Recomendo remover esse campo e passar o `Guid` como parte dos parametos dos métodos privados.

### Geral
  - Evitar comentários desnecessários no código. O código deve ser autoexplicativo.
  - Aumentar a cobertura de testes para atingir 80% ou mais.
  - Remover código morto e arquivos não utilizados.
  - Remover `usings` não utilizados.
  - Remover todas as `#Region`, ele dificulta a leitura do código.
  - Remover métodos não utilizados.
  - Existe variação nas _namespaces_. Espera-se que todas comecem com `MBA.Modulo4`.
  - Quando usar contrutor primário, evite o use de propriedades privadas.

## Eficiência e Desempenho 20%

Avalie o desempenho e a eficiência das soluções implementadas.
* Será revisado na avalição final.
  - Propage o `CancellationToken` em todos os métodos assíncronos.

## Inovação e Diferenciais 10%

Considere a criatividade e inovação na solução proposta.
* Será revisado na avalição final.


## Documentação e Organização 10%

Verifique a qualidade e completude da documentação, incluindo README.md.

- Antes de submeterem para avaliação final, atualizem o README.md e repitam o processo de execução do projeto, para garantir que todas as instruções estão corretas e atualizadas.
- Não é necessário separar testes em Unit e Integration, mas separe por contexto.
- Mantenha a estrutura de pastas conforme o padrão sugerido: 
  - `<Solução>/<Contexto>/<Projeto>`
  - Namespace: `MBA.Modulo4.<Contexto>.<Projeto>`. Ex: `MBA.Modulo4.Alunos.Api`
  - Arquivo de projeto: `./src/MBA.Modulo4/Alunos/Api.csproj` ou `./src/MBA.Modulo4/Alunos/Alunos.Api.csproj` 
  - Pastas da Solução: `MBA.Modulo4/Alunos/Api`

```bash
├── MBA.Modulo4.sln # Arquivo de solução na raiz do repositório
├── src/
│   ├── Alunos/ # Nome do Contexto
│   │   ├── Api/Api.csproj # Projeto de API
│   │   ├── Application/Application.csproj # Projeto de Aplicação
│   │   ├── Domain/Domain.csproj # Projeto de Domínio
.   .   .
│   ├── Conteudo/ # Nome do Contexto
│   │   ├── Api/Api.csproj
.   .   .
├── tests/ # Pasta de Testes, sob ./src
│   ├── Core.Tests.csproj # Projeto de Testes Comuns
│   ├── Alunos.Tests.csproj # Projeto de Testes do Contexto Alunos
│   ├── Conteudo.Tests.csproj # Projeto de Testes do Contexto Conteudo
.   .
```

E na "Solution Explorer" do Visual Studio:
```
MBA.Modulo4
├── Alunos 
│   ├── Api
│   ├── Application
│   ├── Domain
│   ├── Data
.   .
├── Conteudo
│   ├── Api
.   .
├── Tests
│   ├── Alunos
│   ├── Conteudo
.   .
```

## Resolução de Feedbacks 10%

Avalie a resolução dos problemas apontados na primeira avaliação de frontend
* Será revisado na avalição final.
