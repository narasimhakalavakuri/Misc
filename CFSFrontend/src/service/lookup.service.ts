
import { apiClient } from './api.client';
import { Currency, CustomerAccount, Department } from '../common/types';

class LookupService {
  async searchCurrencies(searchTerm: string): Promise<Currency[]> {
    const response = await apiClient.get<any>('/lookups/currencies', {
      params: { search: searchTerm }
    });
    return response.data || [];
  }

  async searchCustomerAccounts(searchTerm: string): Promise<CustomerAccount[]> {
    const response = await apiClient.get<any>('/lookups/accounts', {
      params: { search: searchTerm }
    });
    return response.data || [];
  }

  /**
   * Get account details by account number
   * This is called when the user finishes typing an account code
   */
  async getAccountDetails(accountNo: string): Promise<CustomerAccount> {
    try {
      console.log(`[Lookup Service] Fetching account details for: ${accountNo}`);
      
      const response = await apiClient.get<any>(`/lookups/accounts/${accountNo.toUpperCase()}`);
      
      console.log(`[Lookup Service] Response:`, response);
      
      if (response.success === false) {
        throw new Error(response.error || 'Account not found');
      }
      
      const account: CustomerAccount = {
        accountNo: response.accountNo,
        customerName: response.customerName,
        abbreviatedName: response.abbreviatedName,
      };
      
      console.log(`[Lookup Service] Account found:`, account);
      return account;
    } catch (error: any) {
      console.error(`[Lookup Service] Error fetching account ${accountNo}:`, error);
      throw error;
    }
  }

  async getDepartments(): Promise<Department[]> {
    const response = await apiClient.get<any>('/lookups/departments');
    return response.data || [];
  }

  async getServerDate(): Promise<string> {
    const response = await apiClient.get<any>('/lookups/server-date');
    return response.date || new Date().toISOString().split('T')[0];
  }
}

export const lookupService = new LookupService();
