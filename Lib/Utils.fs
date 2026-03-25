namespace Etl

open System
open Etl.Models

module Utils =

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