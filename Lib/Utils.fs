namespace ETL

/// <summary>
/// Utilitários genéricos usados pelo pipeline ETL.
/// </summary>
module Utils =

    /// <summary>
    /// Realiza um *inner join* entre duas listas (tabelas) com base em chaves extraídas por funções.
    /// </summary>
    /// <param name="getLeftKey">Função que extrai a chave de um elemento da tabela esquerda (`'a -> 'key`).</param>
    /// <param name="getRightKey">Função que extrai a chave de um elemento da tabela direita (`'b -> 'key`).</param>
    /// <param name="joinFun">Função que combina um item da esquerda com um item da direita em um valor de saída (`'a -> 'b -> 'c`).</param>
    /// <param name="leftTable">Lista de elementos da tabela esquerda.</param>
    /// <param name="rightTable">Lista de elementos da tabela direita.</param>
    /// <returns>Lista de resultados do tipo `c` para cada par de elementos cujas chaves coincidem.</returns>
    /// <remarks>
    /// A função é curried e suporta *partial application* — por exemplo, pode-se pré-definir as funções de chave
    /// e reutilizar o resultado em diferentes pares de tabelas. A implementação agrupa a tabela direita por chave
    /// para busca eficiente e depois coleta os pares correspondentes.
    /// </remarks>
    let innerJoin (getLeftKey: 'a -> 'key) (getRightKey: 'b -> 'key) (joinFun: 'a -> 'b -> 'c) (leftTable: 'a list) (rightTable: 'b list): 'c list=
        // Obtem um Map cuja as chaves são os valores (únicos) da coluna ON
        // e os valores todos os itens que possuem aquele valor na coluna ON
        let groupedRightItemsByKey = 
            rightTable 
            |> List.groupBy getRightKey 
            |> Map.ofList
        // Para cada item de Left, pega alista de rightItems com a mesma key
        // E cria uma nova de 'c para cada rightItem
        // OBS.: o resultado vai ser ('c list list), mas o List.collect achata para ('c list)
        leftTable 
        |> List.collect (fun leftItem ->
            let key = getLeftKey leftItem
            match Map.tryFind key groupedRightItemsByKey with
            | Some matchingRightItems -> 
                matchingRightItems 
                |> List.map (joinFun leftItem) // (fun rightItem -> joinFun leftItem rightItem)
            | None -> []
        )