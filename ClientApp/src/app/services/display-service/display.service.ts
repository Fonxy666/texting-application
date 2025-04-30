import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class DisplayService {

    constructor() { }

    displayRemainingTime(time: Date) {
        const currentTime = new Date();

        let years = currentTime.getFullYear() - time.getFullYear();
        let months = currentTime.getMonth() - time.getMonth();
        let days = currentTime.getDate() - time.getDate();
        let hours = currentTime.getHours() - time.getHours();
        let minutes = currentTime.getMinutes() - time.getMinutes();

        if (minutes < 0) {
            minutes += 60;
            hours--;
        }
        if (hours < 0) {
            hours += 24;
            days--;
        }
        if (days < 0) {
            const daysInPreviousMonth = new Date(currentTime.getFullYear(), currentTime.getMonth(), 0).getDate();
            days += daysInPreviousMonth;
            months--;
        }
        if (months < 0) {
            months += 12;
            years--;
        }

        const parts = [];
        if (years > 0) parts.push(`${years} year${years !== 1 ? 's' : ''}`);
        if (months > 0) parts.push(`${months} month${months !== 1 ? 's' : ''}`);
        if (days > 0) parts.push(`${days} day${days !== 1 ? 's' : ''}`);
        if (hours > 0) parts.push(`${hours} hour${hours !== 1 ? 's' : ''}`);
        if (minutes > 0) parts.push(`${minutes} minute${minutes !== 1 ? 's' : ''}`);

        return parts.join(', ');
    }

    displayUserName(name: string) {
        if (!name) {
            return 'Unknown';
        }
        
        if (name.length <= 8) {
            return name;
        } else {
            return name.slice(0, 8) + '...';
        }
    }

    displayMessage(message: string) {
        const maxLength = 35;
        let result = '';

        for (let i = 0; i < message.length; i += maxLength) {
            result += message.slice(i, i + maxLength) + '<br>';
        }

        result = result.slice(0, -4);
        
        return result;
    }
}
