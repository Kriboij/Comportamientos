
using Unity.VisualScripting;
using UnityEngine.UI;

//Predicado: devuelve sí o no
public interface IPredicate
 {
        bool Evaluate();
 }

//Transición