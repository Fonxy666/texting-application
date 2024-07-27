import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const passwordValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
    const password = control.value;
    const regex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;

    if (!regex.test(password)) {
        return { invalidPassword: true };
    }

    return null;
};

export function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password')?.value;
    const passwordRepeat = control.get('passwordrepeat')?.value;

    if (!password || !passwordRepeat) {
        return null;
    }

    if (password !== passwordRepeat) {
        return { passwordsNotMatch: true };
    }

    return null;
}

export function decryptTokenValidator(control: AbstractControl): ValidationErrors | null {
    const token = control.value;

    if (token && /^\d{6}$/.test(token)) {
        return null;
    } else {
        return { invalidToken: true };
    }
}
