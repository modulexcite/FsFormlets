﻿namespace Formlets

type 'a ErrorList = string list * 'a

module ErrorList =
    let inline puree v = [],v
    let ap (x: 'a ErrorList) (f: ('a -> 'b) ErrorList) : 'b ErrorList =
        let ff, sf = f
        let fx, sx = x
        fx @ ff, sf sx
    let inline (<*>) f x = ap x f
    let inline map f x = puree f <*> x
    let inline lift2 f x y = puree f <*> x <*> y
    let inline append v (e: 'a ErrorList): 'a ErrorList = (fst e) @ v, snd e
