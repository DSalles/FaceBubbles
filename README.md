# FaceBubbles
blow bubbles with sound
XNA application I made as an experiment
When the user spins the mousewheel, the app generates bubbles with the user's face centered in them.
The bubbles show video and audio of the user. Each bubble shows live video and audio in its inception,
but after a number of seconds, it replays the recorded audio and video.
Since the inception time of each bubble is unique, each one has video that, once it starts to repeat, 
is slightly offset in time. There can be up to a couple hundred bubbles in view at once,
each following its own windy bubble-like path.
Kinect face tracking is used to keep the user's face inside the capture frame.'
I wrote HLSL shaders to create the bubble, the vignetting for the video, as well as color effects.
