### Feedback

*Please add below any feedback you want to send to the team*

Provided API: 
1. Movie Search is not searching by complete title, but by partial title. 
     For example, searching for "The mock movie" returns a 500 status code, but searching with "mock" returns a record.
2. It should have a release date property in the response, currently it has only the YEAR.
3. It should have a movie length property in the response. That could be used to calculate the showtime.By this,We can add validation for auditorium availability by checking for overlapping showtimes.


Requirements:    
 1. When making a reservation, all selected seats must be contiguous, starting from any available seat number. This requirement may lead to unoccupied seats in the auditorium.
    Example: Suppose the auditorium has 10 seats. A user reserves 8 seats (seats 1–8). Now, another user wants to reserve 4 seats, but the only available seats are 1, 2, 9, and 10. Since the seats must be contiguous, the user cannot book all 4 seats in a single reservation and will have to purchase two separate tickets instead.