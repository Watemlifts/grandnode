using Grand.Core.Caching;
using Grand.Core.Data;
using Grand.Core.Domain.Stores;
using Grand.Services.Events;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Services.Stores
{
    /// <summary>
    /// Store service
    /// </summary>
    public partial class StoreService : IStoreService
    {
        #region Constants

        /// <summary>
        /// Key for caching
        /// </summary>
        private const string STORES_ALL_KEY = "Grand.stores.all";
        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : store ID
        /// </remarks>
        private const string STORES_BY_ID_KEY = "Grand.stores.id-{0}";
        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        private const string STORES_PATTERN_KEY = "Grand.stores.";

        #endregion
        
        #region Fields
        
        private readonly IRepository<Store> _storeRepository;
        private readonly IMediator _mediator;
        private readonly ICacheManager _cacheManager;

        private List<Store> _allStores;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="storeRepository">Store repository</param>
        /// <param name="eventPublisher">Event published</param>
        public StoreService(ICacheManager cacheManager,
            IRepository<Store> storeRepository,
            IMediator mediator)
        {
            this._cacheManager = cacheManager;
            this._storeRepository = storeRepository;
            this._mediator = mediator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Deletes a store
        /// </summary>
        /// <param name="store">Store</param>
        public virtual async Task DeleteStore(Store store)
        {
            if (store == null)
                throw new ArgumentNullException("store");

            var allStores = await GetAllStores();
            if (allStores.Count == 1)
                throw new Exception("You cannot delete the only configured store");

            await _storeRepository.DeleteAsync(store);

            //clear cache
            await _cacheManager.Clear();

            //event notification
            await _mediator.EntityDeleted(store);
        }

        /// <summary>
        /// Gets all stores
        /// </summary>
        /// <returns>Stores</returns>
        public virtual async Task<IList<Store>> GetAllStores()
        {
            if (_allStores == null)
            {
                string key = STORES_ALL_KEY;
                _allStores = await _cacheManager.GetAsync(key, () =>
                {
                    return _storeRepository.Collection.Find(new BsonDocument()).SortBy(x => x.DisplayOrder).ToListAsync();
                });
            }
            return _allStores;
        }

        /// <summary>
        /// Gets a store 
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Store</returns>
        public virtual Task<Store> GetStoreById(string storeId)
        {
            string key = string.Format(STORES_BY_ID_KEY, storeId);
            return _cacheManager.GetAsync(key, () => _storeRepository.GetByIdAsync(storeId));
        }
        
        /// <summary>
        /// Inserts a store
        /// </summary>
        /// <param name="store">Store</param>
        public virtual async Task InsertStore(Store store)
        {
            if (store == null)
                throw new ArgumentNullException("store");

            await _storeRepository.InsertAsync(store);

            //clear cache
            await _cacheManager.Clear();

            //event notification
            await _mediator.EntityInserted(store);
        }

        /// <summary>
        /// Updates the store
        /// </summary>
        /// <param name="store">Store</param>
        public virtual async Task UpdateStore(Store store)
        {
            if (store == null)
                throw new ArgumentNullException("store");

            await _storeRepository.UpdateAsync(store);

            //clear cache
            await _cacheManager.Clear();

            //event notification
            await _mediator.EntityUpdated(store);
        }

        /// <summary>
        /// Gets a store mapping 
        /// </summary>
        /// <param name="discountId">Discount id mapping identifier</param>
        /// <returns>store mapping</returns>
        public virtual async Task<IList<Store>> GetAllStoresByDiscount(string discountId)
        {
            var query = from c in _storeRepository.Table
                        where c.AppliedDiscounts.Any(x => x == discountId)
                        select c;
            return await query.ToListAsync();
        }
        #endregion
    }
}