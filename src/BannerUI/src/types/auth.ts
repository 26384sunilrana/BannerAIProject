export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  password: string
  firstName: string
  lastName: string
  shopName: string
  city?: string
  phoneNumber?: string
}

export interface AuthResult {
  message: string
  tokens: {
    accessToken: string
    /** Empty: the refresh token travels in an HttpOnly cookie. */
    refreshToken?: string
    expiresIn: number
    tokenType: string
  }
}
