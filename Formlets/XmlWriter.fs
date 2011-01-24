﻿namespace Formlets

open System.Xml.Linq

type 'a XmlWriter = XNode list * 'a

[<AutoOpen>]
module XmlHelpers =
    /// Gets attributes of an element as a tuple list
    let getAttr (e: XElement) =
        e.Attributes() 
        |> Seq.map (fun a -> a.Name.LocalName,a.Value)
        |> Seq.toList

    /// <summary>
    /// Matches a <see cref="XElement"/>
    /// </summary>
    /// <param name="n"></param>
    let (|Tag|_|) (n: XNode) = 
        match n with
        | :? XElement as e -> Some e
        | _ -> None

    /// <summary>
    /// Matches a <see cref="XElement"/>, splitting name, attributes and children
    /// </summary>
    /// <param name="n"></param>
    let (|TagA|_|) (n : XNode) =
        match n with
        | Tag t -> 
            let name = t.Name.LocalName
            let attr = getAttr t
            let children = t.Nodes() |> Seq.toList
            Some(name,attr,children)
        | _ -> None

    /// <summary>
    /// Matches a <see cref="XText"/>
    /// </summary>
    /// <param name="n"></param>
    let (|Text|_|) (n: XNode) =
        match n with
        | :? XText as t -> Some t
        | _ -> None

    /// <summary>
    /// Matches a <see cref="XText"/>, extracting the actual text value
    /// </summary>
    /// <param name="n"></param>
    let (|TextV|_|) (n: XNode) =
        match n with
        | Text t -> Some t.Value
        | _ -> None

    /// <summary>
    /// Matches a <see cref="XComment"/>
    /// </summary>
    /// <param name="n"></param>
    let (|Comment|_|) (n: XNode) =
        match n with
        | :? XComment as c -> Some c
        | _ -> None

    /// <summary>
    /// Matches a <see cref="XComment"/>, extract the actual text value
    /// </summary>
    /// <param name="n"></param>
    let (|CommentV|_|) (n: XNode) =
        match n with
        | Comment c -> Some c.Value
        | _ -> None

/// Applicative functor that manipulates HTML as XML
module XmlWriter =
    let emptyElems = ["area";"base";"basefont";"br";"col";"frame";"hr";"img";"input";"isindex";"link";"meta";"param"]
    let inline (!!) x = XName.op_Implicit x
    let inline xattr (name, value: string) = XAttribute(!!name, value)
    let xelem name (attributes: (string*string) list) (children: XNode list) = 
        let isEmpty = List.exists ((=) name) emptyElems
        let children = 
            match children,isEmpty with
            | [],false -> [(XText "") :> XObject]
            | _ -> List.map (fun x -> upcast x) children
        let attributes = List.map (fun a -> xattr a :> XObject) attributes
        XElement(!!name, attributes @ children) :> XNode

    let inline puree v : 'a XmlWriter = [],v
    //let ap (x: xml_item list,f) (y,a) = x @ y, f a
    let ap (f: ('a -> 'b) XmlWriter) (x: 'a XmlWriter) : 'b XmlWriter =
        let ff = fst f
        let sf = snd f
        let fx = fst x
        let sx = snd x
        ff @ fx, sf sx
    let inline (<*>) f x = ap f x
    let inline lift f x = puree f <*> x
    let inline lift2 f x y = puree f <*> x <*> y
    let inline plug (k: XNode list -> XNode list) (v: 'a XmlWriter): 'a XmlWriter = 
        k (fst v), snd v
    let inline xml (e: XNode list) : unit XmlWriter = e,()
    let inline text (s: string) = xml [XText s]
    let inline tag name attributes (v: 'a XmlWriter) : 'a XmlWriter = 
        plug (fun x -> [xelem name attributes x]) v

    let inline xnode (e: XNode) : unit XmlWriter = [e],()
    let wrap (n: XNode list) =
        match n with
        | [] -> failwith "empty list"
        | [x] -> x
        | x -> xelem "div" [] x