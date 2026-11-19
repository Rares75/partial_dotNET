# AI-LOG

Write down, in order, what you asked the AI. It is not a punishment: a missing or false log lowers the level D grade.

| Time (approx.) | What you asked (briefly) | What you kept | What you changed or rejected |
|---|---|---|---|
| 8m 14 s| use skill that you will find in the partial_dotNET project in order to
  modify the code from roomhub-starter directory. Also read the
  ASSIGNMENT.md to figure out what my project requirments are | The AI found and fix the bugs from the initial code + the bugs i identified myself (i will specify them in the BUGS.md file), i keep the bug fixing, the refactored src/RoomHub.Api to a verstical slice stack: Feataures/ , Domain/Common/, Dommain/Rooms,Dommain/bookings, Common/CQRS/ Infrastructre/Persistance and TimeProvider.System| I wasnt needed to change the code myself, i check the generated code and all the bug fixes and the AI generated code made them correctly, i rejected the implemenation of the unit tests and Postman tests the AI suggested since they are not  specifiend in the ASSIGNMENT.md file |

|11m 21 s|i give the AI the full description of the P9 requirment|I keep the BookingErros.cs,IBookingRepository.cs the AI has modified. I also checked the Test AI made using curls |I was needed to add the correct translation of the time to UTC since the AI has made them wrong|

|1m 28s| i give the AI the description of the task R10|I keep all the code AI has generated except those i mentioned|I changed the AI to make my decision: to replace the MaxDuration constant with a capacity-dependent limit and makes sure that it will not interfere with the R6.|


|1m 42 s|I give the AI the full description of the R11 and also the mention that this should not be in conflict with "409" error code that can came from "too close to start" and "already cance;ed case|I checked the changes AI has made in order to be sure that he also treated the 409 casses i mentioned|since everything was taken in consideration by AI and the build passed i kept every modification|