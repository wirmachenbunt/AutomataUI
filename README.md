# AutomataUI for vvvv

Hello Automata UI !
this is a visual finite state machine editor with built in finite state machine.

You want bulletproof logic ? You better use the automata node! Well, that's what bjoern told me a couple of years ago. And he was right. If your patch becomes more and more complex, more and more bugs turn up due monoflop, delay, flipflop and framedelay madness. It's much easier with a statemachine.

But since it is probably no fun writing quadruples, it makes sense to introduce a visual editor.

Some Features

So here it is. Inspired by bjoern's "Timer" concept, transitions not only last a frame but can have a configurable duration. Quite useful for any animated user interface project, where animated transitions should be in sync with your logic.

Transitions can be pingpong, meaning they can be bi-directional. States on the other hand have a duration as well, blocking any outgoing Transition for a certain amount of time.

More rules are in the help patch.
