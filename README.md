Click anywhere on the grid to raise the height of the water at that point.
Releasing or moving the mouse causes waves according to simplified
shallow water equations.
Simulating shallow water with these simplified equations allows greater processing
speed under the threat of instability.

The stability variables I settled on are:

Default height: 0.5  
Height set on click: 5    (setting the height is stable, adding height on mouse-pressed while sampling at ~60 times a second is massively unstable)  
Gravity: 0.03  
Viscosity: 0.2  
TimeStep: elapsedTimeSpan.Ticks / 100000    (elapsedTimeSpan comes out of the gameTime passed into the Update method)